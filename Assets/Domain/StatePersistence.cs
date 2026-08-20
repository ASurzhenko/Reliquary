using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// Save on change, and the single write behind every exchange. Listening to the inventory rather than
    /// being called by whoever changed it makes persistence a property of the state instead of a discipline
    /// every future call site must remember; owning the exchange write is what lets a copy and a balance
    /// move together or not at all.
    /// </summary>
    public sealed class StatePersistence : IDisposable
    {
        private readonly IInventoryStore _store;
        private readonly Inventory _inventory;
        private readonly EssenceWallet _wallet;
        private readonly IReadOnlyList<InventorySnapshotEntry> _carried;

        private bool _committing;
        private bool _disposed;

        public StatePersistence(IInventoryStore store, Inventory inventory, EssenceWallet wallet,
            IReadOnlyList<InventorySnapshotEntry> carried)
        {
            _store = store;
            _inventory = inventory;
            _wallet = wallet;
            _carried = carried;
            _inventory.Changed += OnInventoryChanged;
        }

        /// <summary>
        /// A write that did not land on the auto-save path. The state is in memory and the next successful
        /// save re-persists it. Developer-facing reason; never player copy.
        /// </summary>
        public event Action<string> SaveFailed;

        /// <summary>
        /// A write that did not land inside an exchange, where nothing was applied. The opposite meaning to
        /// SaveFailed, which is why it is a second channel rather than a second sentence on the first.
        /// </summary>
        public event Action<string> ApplyRefused;

        /// <summary>
        /// Writes the whole state, not a delta, so a failed write self-heals: the next successful one
        /// re-persists everything. Public because a kind of state the inventory does not raise a change for
        /// still has to reach the disk.
        /// </summary>
        public void Save()
        {
            InventorySnapshot snapshot = InventorySnapshotFactory.Create(_inventory, _wallet, _carried);

            if (!CarriesState(snapshot))
            {
                SaveFailed?.Invoke("refused to write a save with no state — the next boot would not accept it");
                return;
            }

            if (!_store.TrySave(snapshot, out string failure))
            {
                SaveFailed?.Invoke(failure);
            }
        }

        /// <summary>
        /// Persists the state the change WOULD produce, and only then applies it. Nothing is spent until the
        /// whole resulting state is on disk, so a refused write costs the player a tap rather than a copy,
        /// and a crash between the write and the mutation resolves on the next boot to the completed
        /// exchange — the disk holds both halves of it.
        /// </summary>
        public bool TryApply(StateChange change, out string failure)
        {
            if (_committing)
            {
                // A Changed handler starting a second exchange would build its snapshot from half-announced
                // state. Nothing in this project does it; the day something does, it says so instead of
                // corrupting.
                throw new InvalidOperationException(
                    "A state change was started from inside another one. Exchanges are not re-entrant.");
            }

            InventorySnapshot prospective =
                InventorySnapshotFactory.CreateWith(_inventory, _wallet, _carried, change);

            if (!CarriesState(prospective))
            {
                failure = "refused to write a save with no state — the next boot would not accept it";
                ApplyRefused?.Invoke(failure);
                return false;
            }

            if (!_store.TrySave(prospective, out failure))
            {
                ApplyRefused?.Invoke(failure);
                return false;                   // nothing has been mutated
            }

            _committing = true;

            try
            {
                // Both mutations first, both silent. Neither can throw here: the caller validated the change
                // and re-validated it against these same objects a moment ago, and the guards inside them are
                // the last line rather than the first.
                InventoryChange inventoryChange = default;
                bool movedACopy = change.CopyDelta != 0;

                if (movedACopy)
                {
                    inventoryChange = _inventory.ApplySilently(change.Relic, change.CopyDelta);
                }

                EssenceChange essenceChange =
                    _wallet.ApplySilently(change.EssenceDelta, change.Reason, change.Relic);

                // Essence first: a purchase reads as "the essence left, and then the relic arrived", which is
                // the order the player performed it in.
                _wallet.Announce(essenceChange);

                // A change that moved no copy raises no InventoryChange at all. Subscribers key on the sign
                // of Delta, and none of them has a branch for zero.
                if (movedACopy)
                {
                    _inventory.Announce(inventoryChange);
                }
            }
            finally
            {
                _committing = false;
            }

            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _inventory.Changed -= OnInventoryChanged;

            // A subscriber that forgot to unsubscribe must not keep this object alive.
            SaveFailed = null;
            ApplyRefused = null;
        }

        /// <summary>
        /// SavedInventoryReader refuses a save that carries no state, on the argument that this build never
        /// writes one. Both writers ask this, so the argument does not depend on which callers happen to
        /// exist: StateChange.Grant accepts any amount, including one that empties the balance.
        /// </summary>
        private static bool CarriesState(InventorySnapshot snapshot)
        {
            return (snapshot.Entries != null && snapshot.Entries.Length > 0) || snapshot.Essence > 0;
        }

        private void OnInventoryChanged(InventoryChange change)
        {
            if (_committing)
            {
                return;
            }

            Save();
        }
    }
}
