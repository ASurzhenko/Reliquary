using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// How far an inventory is through a set. Computed from the set and the inventory every time it is
    /// asked for, and stored nowhere: a saved copy would be a second source of truth that a partially
    /// readable save could contradict.
    /// </summary>
    public readonly struct SetProgress
    {
        private readonly IReadOnlyList<RelicId> _missing;

        private SetProgress(SetId id, int owned, int total, IReadOnlyList<RelicId> missing)
        {
            Id = id;
            Owned = owned;
            Total = total;
            _missing = missing;
        }

        public static SetProgress For(RelicSet set, Inventory inventory)
        {
            if (set == null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            IReadOnlyList<RelicId> members = set.Members;
            List<RelicId> missing = new List<RelicId>(members.Count);
            int owned = 0;

            for (int i = 0; i < members.Count; i++)
            {
                if (inventory.Owns(members[i]))
                {
                    owned++;
                    continue;
                }

                missing.Add(members[i]);
            }

            return new SetProgress(set.Id, owned, members.Count, missing);
        }

        public SetId Id { get; }

        /// <summary>Members the inventory owns at least one copy of. A duplicate does not advance a set.</summary>
        public int Owned { get; }

        public int Total { get; }

        public bool IsComplete => Total > 0 && Owned == Total;

        public bool IsUnstarted => Owned == 0;

        /// <summary>
        /// Owned over total, 0 for a set with no members. On the struct rather than left to the caller so
        /// that no view divides two domain values or compares one against zero.
        /// </summary>
        public float Fraction => Total == 0 ? 0f : Owned / (float)Total;

        /// <summary>Members not owned, in author order. The Trader's pool is drawn from these.</summary>
        public IReadOnlyList<RelicId> Missing => _missing ?? Array.Empty<RelicId>();
    }
}
