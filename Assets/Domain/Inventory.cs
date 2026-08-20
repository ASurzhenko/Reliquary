using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// What the player owns, and how many copies of each. Identity is a RelicId — never a position in a
    /// list, because the catalogue's order is not guaranteed and both halves are filtered independently.
    /// </summary>
    public sealed class Inventory
    {
        private readonly Dictionary<RelicId, int> _counts;

        public Inventory()
        {
            _counts = new Dictionary<RelicId, int>();
        }

        /// <summary>
        /// Restores counts read from a save. The reader is what filters malformed entries; the guards here
        /// are the last line, for a caller that skipped it.
        /// </summary>
        public Inventory(IEnumerable<InventoryEntry> restored)
            : this()
        {
            if (restored == null)
            {
                return;
            }

            foreach (InventoryEntry entry in restored)
            {
                if (!entry.Id.IsValid)
                {
                    throw new ArgumentException("A restored entry has no id.", nameof(restored));
                }

                if (entry.Count <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(restored), entry.Count,
                        $"'{entry.Id}' was restored with a count of {entry.Count}. Ownership starts at 1.");
                }

                if (_counts.ContainsKey(entry.Id))
                {
                    throw new ArgumentException($"'{entry.Id}' appears twice in the restored entries.", nameof(restored));
                }

                _counts.Add(entry.Id, entry.Count);
            }
        }

        /// <summary>Raised once per accepted change, after the count has been updated.</summary>
        public event Action<InventoryChange> Changed;

        /// <summary>How many different relics are owned. The numerator of "7 of 12 found".</summary>
        public int DistinctCount => _counts.Count;

        public bool Owns(RelicId id) => _counts.ContainsKey(id);

        public int CountOf(RelicId id) => _counts.TryGetValue(id, out int count) ? count : 0;

        /// <summary>Owned relics in a stable order: ordinal by id, so a save is diffable and a view is calm.</summary>
        public IReadOnlyList<InventoryEntry> Entries()
        {
            InventoryEntry[] entries = new InventoryEntry[_counts.Count];
            int index = 0;

            foreach (KeyValuePair<RelicId, int> pair in _counts)
            {
                entries[index] = new InventoryEntry(pair.Key, pair.Value);
                index++;
            }

            Array.Sort(entries, (left, right) => string.CompareOrdinal(left.Id.ToString(), right.Id.ToString()));
            return entries;
        }

        public InventoryChange Add(RelicId id)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("A relic without an id cannot be owned.", nameof(id));
            }

            bool firstCopy = !_counts.TryGetValue(id, out int count);
            _counts[id] = count + 1;

            InventoryChange change = new InventoryChange(id, count + 1, firstCopy);
            Changed?.Invoke(change);
            return change;
        }
    }
}
