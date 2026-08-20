using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>Relative chances for one acquisition. Built per draw; nothing is cached across requests.</summary>
    public sealed class DrawTable
    {
        private readonly RelicId[] _ids;
        private readonly int[] _cumulativeWeights;

        private DrawTable(RelicId[] ids, int[] cumulativeWeights, int totalWeight)
        {
            _ids = ids;
            _cumulativeWeights = cumulativeWeights;
            TotalWeight = totalWeight;
        }

        public int TotalWeight { get; }

        /// <summary>
        /// A relic's chance is its discovery weight, plus a pull bonus while the player does not own it —
        /// the hook a set-completion perk grants, without the draw needing to know a set exists.
        /// </summary>
        public static DrawTable Build(RelicCatalog catalog, Inventory inventory, RelicModifiers modifiers)
        {
            IReadOnlyList<Relic> relics = catalog.All;
            List<RelicId> ids = new List<RelicId>(relics.Count);
            List<int> cumulative = new List<int>(relics.Count);
            int unownedBonus = modifiers == null ? 0 : modifiers.UnownedPullBonus;
            int total = 0;

            for (int i = 0; i < relics.Count; i++)
            {
                Relic relic = relics[i];
                int weight = relic.DiscoveryWeight + (inventory.Owns(relic.Id) ? 0 : unownedBonus);

                if (weight <= 0)
                {
                    continue;
                }

                total += weight;
                ids.Add(relic.Id);
                cumulative.Add(total);
            }

            return new DrawTable(ids.ToArray(), cumulative.ToArray(), total);
        }

        /// <summary>
        /// Picks the relic whose weight band contains <paramref name="roll"/>.
        /// Throws ArgumentOutOfRangeException unless 0 &lt;= roll &lt; TotalWeight.
        /// </summary>
        public RelicId Pick(int roll)
        {
            if (roll < 0 || roll >= TotalWeight)
            {
                throw new ArgumentOutOfRangeException(nameof(roll), roll,
                    $"A roll must be at least 0 and below the total weight of {TotalWeight}.");
            }

            for (int i = 0; i < _cumulativeWeights.Length; i++)
            {
                if (roll < _cumulativeWeights[i])
                {
                    return _ids[i];
                }
            }

            throw new InvalidOperationException($"Roll {roll} fell outside every weight band. The table is inconsistent.");
        }
    }
}
