using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// Announces a set the moment it completes. The perk itself is not granted here — it is derived from
    /// ownership every time it is read, so there is no "granted twice" to prevent. What happens exactly once
    /// is the announcement, and it is made once without persisting a flag: the seed below is the
    /// persistence, because the save already records the ownership completion is a function of.
    /// </summary>
    public sealed class SetCompletionWatcher : IDisposable
    {
        private readonly SetCatalog _sets;
        private readonly Inventory _inventory;
        private readonly HashSet<SetId> _completed;

        private bool _disposed;

        public SetCompletionWatcher(SetCatalog sets, Inventory inventory)
        {
            _sets = sets ?? throw new ArgumentNullException(nameof(sets));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));

            // Seeded from the state this session booted with. A set completed in an earlier session is
            // already in here, so it has no transition left to make and no card left to show.
            _completed = new HashSet<SetId>(sets.CompleteIn(inventory));
            _inventory.Changed += OnInventoryChanged;
        }

        /// <summary>
        /// Raised once per completion transition in this session, seeded from the completions the session
        /// booted with. Carries the SetId; the display name and the perk lines are the presentation layer's.
        /// </summary>
        public event Action<SetCompletion> Completed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _inventory.Changed -= OnInventoryChanged;
            Completed = null;
        }

        private void OnInventoryChanged(InventoryChange change)
        {
            // Only the sets holding the relic that moved can have changed state, which is what the catalogue
            // precomputed its member index for.
            IReadOnlyList<RelicSet> holders = _sets.SetsContaining(change.Id);

            for (int i = 0; i < holders.Count; i++)
            {
                RelicSet set = holders[i];

                if (!SetProgress.For(set, _inventory).IsComplete)
                {
                    continue;
                }

                if (!_completed.Add(set.Id))
                {
                    continue;
                }

                Completed?.Invoke(new SetCompletion(set.Id));
            }
        }
    }
}
