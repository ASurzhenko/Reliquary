using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// How the vault reads what is owned. The order is the screen's whole reason to exist — the copies a
    /// player can spare come first — so it is a rule, and it is tested.
    /// </summary>
    public static class VaultQuery
    {
        /// <summary>
        /// Owned relics, most spare copies first, then ordinal by id so equal counts have a stable order. An
        /// id that is not in this build's catalogue is omitted: there is no name or icon to draw it with.
        /// </summary>
        public static IReadOnlyList<InventoryEntry> Order(RelicCatalog catalog, Inventory inventory)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            IReadOnlyList<InventoryEntry> owned = inventory.Entries();
            List<InventoryEntry> ordered = new List<InventoryEntry>(owned.Count);

            for (int i = 0; i < owned.Count; i++)
            {
                if (catalog.Contains(owned[i].Id))
                {
                    ordered.Add(owned[i]);
                }
            }

            ordered.Sort(Compare);
            return ordered;
        }

        /// <summary>Whether any relic is owned more than once. The vault's sub-header branches on this.</summary>
        public static bool HasAnySpares(Inventory inventory)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            IReadOnlyList<InventoryEntry> owned = inventory.Entries();

            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i].Count > 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>How many copies could be spared in total: every copy beyond the first of each relic.</summary>
        public static int SpareCopies(Inventory inventory)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            IReadOnlyList<InventoryEntry> owned = inventory.Entries();
            int spares = 0;

            for (int i = 0; i < owned.Count; i++)
            {
                spares += owned[i].Count - 1;
            }

            return spares;
        }

        private static int Compare(InventoryEntry left, InventoryEntry right)
        {
            int byCount = right.Count.CompareTo(left.Count);

            return byCount != 0 ? byCount : string.CompareOrdinal(left.Id.ToString(), right.Id.ToString());
        }
    }
}
