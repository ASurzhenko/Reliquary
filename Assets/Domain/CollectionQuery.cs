using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// Which relics a filter shows. It lives here rather than in the screen that draws the grid because it is
    /// a rule about ownership, and a rule with two homes is a rule only one of which is tested.
    /// </summary>
    public static class CollectionQuery
    {
        /// <summary>
        /// The catalogue, narrowed to the asked-for part and left in the catalogue's own order: pressing a
        /// filter chip hides tiles, it never moves them.
        /// </summary>
        public static IReadOnlyList<Relic> Filter(RelicCatalog catalog, Inventory inventory, CollectionFilter filter)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            IReadOnlyList<Relic> all = catalog.All;
            List<Relic> shown = new List<Relic>(all.Count);

            for (int i = 0; i < all.Count; i++)
            {
                Relic relic = all[i];

                if (Shows(filter, inventory.Owns(relic.Id)))
                {
                    shown.Add(relic);
                }
            }

            return shown;
        }

        private static bool Shows(CollectionFilter filter, bool owned)
        {
            switch (filter)
            {
                case CollectionFilter.All:
                    return true;

                case CollectionFilter.Owned:
                    return owned;

                case CollectionFilter.Missing:
                    return !owned;

                default:
                    throw new ArgumentOutOfRangeException(nameof(filter), filter,
                        "A filter with no rule cannot answer which relics it shows.");
            }
        }
    }
}
