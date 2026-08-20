using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>What is contributing right now — the only place that decides which effects are live.</summary>
    public static class ActiveModifiers
    {
        /// <summary>
        /// One contribution per DISTINCT owned relic, plus the perks of every completed set. Duplicates do
        /// not stack: a duplicate's value is essence, and letting it also multiply an effect would pay for
        /// the same copy twice. A set's perks arrive through the same accumulator a relic's effects do,
        /// which is what makes the gamification layer a consumer of the effect system rather than a
        /// mechanism bolted beside it.
        /// </summary>
        public static RelicModifiers For(RelicCatalog relics, SetCatalog sets, Inventory inventory)
        {
            List<IRelicEffect> effects = new List<IRelicEffect>();
            IReadOnlyList<InventoryEntry> owned = inventory.Entries();

            for (int i = 0; i < owned.Count; i++)
            {
                if (!relics.TryGet(owned[i].Id, out Relic relic))
                {
                    continue;
                }

                for (int effectIndex = 0; effectIndex < relic.Effects.Count; effectIndex++)
                {
                    effects.Add(relic.Effects[effectIndex]);
                }
            }

            if (sets != null)
            {
                IReadOnlyList<RelicSet> all = sets.All;

                for (int i = 0; i < all.Count; i++)
                {
                    if (!SetProgress.For(all[i], inventory).IsComplete)
                    {
                        continue;
                    }

                    for (int perkIndex = 0; perkIndex < all[i].Perks.Count; perkIndex++)
                    {
                        effects.Add(all[i].Perks[perkIndex]);
                    }
                }
            }

            return RelicModifiers.From(effects);
        }
    }
}
