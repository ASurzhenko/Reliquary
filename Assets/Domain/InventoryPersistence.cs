using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// Save on change. Listening to the inventory rather than being called by whoever changed it makes
    /// persistence a property of the state instead of a discipline every future call site must remember.
    /// </summary>
    public sealed class InventoryPersistence : IDisposable
    {
        private readonly IInventoryStore _store;
        private readonly Inventory _inventory;
        private readonly IReadOnlyList<InventorySnapshotEntry> _carried;

        private bool _disposed;

        public InventoryPersistence(IInventoryStore store, Inventory inventory,
            IReadOnlyList<InventorySnapshotEntry> carried)
        {
            _store = store;
            _inventory = inventory;
            _carried = carried;
            _inventory.Changed += OnInventoryChanged;
        }

        /// <summary>Raised with a developer-facing reason when a write did not land. Never player copy.</summary>
        public event Action<string> SaveFailed;

        /// <summary>
        /// Writes the whole state, not a delta, so a failed write self-heals: the next successful one
        /// re-persists everything. Public because a later kind of state may need to persist a change the
        /// inventory does not raise.
        /// </summary>
        public void Save()
        {
            InventorySnapshot snapshot = InventorySnapshotFactory.Create(_inventory, _carried);

            // SavedInventoryReader refuses a save with no entries, on the argument that this build never
            // writes one. This is where that argument is kept true: refuse the write instead of producing a
            // payload the next boot would discard. Unreachable while the only writer is Inventory.Add.
            if (snapshot.Entries == null || snapshot.Entries.Length == 0)
            {
                SaveFailed?.Invoke("refused to write a save with no entries — the next boot would not accept it");
                return;
            }

            if (!_store.TrySave(snapshot, out string failure))
            {
                SaveFailed?.Invoke(failure);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _inventory.Changed -= OnInventoryChanged;
            SaveFailed = null;
        }

        private void OnInventoryChanged(InventoryChange change)
        {
            Save();
        }
    }
}
