using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>What the relics a player owns add up to.</summary>
    public static class InventoryModifiers
    {
        /// <summary>
        /// Collects the effects of every owned relic. Duplicates do not stack: a second copy is worth
        /// essence, and letting it also multiply an effect would pay for the same copy twice — so each
        /// distinct owned id contributes its effects once.
        /// </summary>
        public static RelicModifiers From(RelicCatalog catalog, Inventory inventory)
        {
            List<IRelicEffect> effects = new List<IRelicEffect>();
            IReadOnlyList<InventoryEntry> owned = inventory.Entries();

            for (int i = 0; i < owned.Count; i++)
            {
                if (!catalog.TryGet(owned[i].Id, out Relic relic))
                {
                    continue;
                }

                for (int effectIndex = 0; effectIndex < relic.Effects.Count; effectIndex++)
                {
                    effects.Add(relic.Effects[effectIndex]);
                }
            }

            return RelicModifiers.From(effects);
        }
    }
}
