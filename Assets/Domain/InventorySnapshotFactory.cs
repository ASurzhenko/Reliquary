using System.Collections.Generic;

namespace Reliquary.Domain
{
    public static class InventorySnapshotFactory
    {
        /// <summary>
        /// The whole persisted state: what is owned now, plus entries carried from a save whose relics this
        /// build does not know. The two sets are disjoint — an entry is carried precisely because the
        /// catalogue does not contain it, and AcquisitionCoordinator only ever adds ids it has checked
        /// against the catalogue — so they are concatenated without a merge.
        /// </summary>
        public static InventorySnapshot Create(Inventory inventory, IReadOnlyList<InventorySnapshotEntry> carried)
        {
            IReadOnlyList<InventoryEntry> owned = inventory.Entries();
            int carriedCount = carried == null ? 0 : carried.Count;

            InventorySnapshot snapshot = new InventorySnapshot
            {
                Version = SaveFormat.Current,
                Entries = new InventorySnapshotEntry[owned.Count + carriedCount]
            };

            for (int i = 0; i < owned.Count; i++)
            {
                snapshot.Entries[i] = new InventorySnapshotEntry
                {
                    RelicId = owned[i].Id.ToString(),
                    Count = owned[i].Count
                };
            }

            for (int i = 0; i < carriedCount; i++)
            {
                snapshot.Entries[owned.Count + i] = new InventorySnapshotEntry
                {
                    RelicId = carried[i].RelicId,
                    Count = carried[i].Count
                };
            }

            return snapshot;
        }
    }
}
