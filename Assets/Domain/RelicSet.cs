using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// A goal made of relics: which ones belong to it, and what completing it grants. The direction is
    /// one-way on purpose — adding a set edits no relic.
    /// </summary>
    public sealed class RelicSet
    {
        public RelicSet(SetId id, IReadOnlyList<RelicId> members, IReadOnlyList<IRelicEffect> perks)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("A set needs a valid id.", nameof(id));
            }

            RelicId[] copiedMembers = members == null ? Array.Empty<RelicId>() : new RelicId[members.Count];

            for (int i = 0; i < copiedMembers.Length; i++)
            {
                if (!members[i].IsValid)
                {
                    throw new ArgumentException($"Member {i} has no id.", nameof(members));
                }

                copiedMembers[i] = members[i];
            }

            IRelicEffect[] copiedPerks = perks == null ? Array.Empty<IRelicEffect>() : new IRelicEffect[perks.Count];

            for (int i = 0; i < copiedPerks.Length; i++)
            {
                copiedPerks[i] = perks[i] ?? throw new ArgumentException($"Perk {i} is null.", nameof(perks));
            }

            Id = id;
            Members = copiedMembers;
            Perks = copiedPerks;
        }

        public SetId Id { get; }

        /// <summary>Member ids in author order. A set knows its relics; a relic knows nothing about sets.</summary>
        public IReadOnlyList<RelicId> Members { get; }

        /// <summary>What completing this set contributes. Authored as the effect type a relic uses.</summary>
        public IReadOnlyList<IRelicEffect> Perks { get; }

        public bool Contains(RelicId id)
        {
            for (int i = 0; i < Members.Count; i++)
            {
                if (Members[i] == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
