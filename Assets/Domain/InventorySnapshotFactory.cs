using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    public static class InventorySnapshotFactory
    {
        /// <summary>
        /// The whole persisted state: what is owned now, the balance, plus entries carried from a save whose
        /// relics this build does not know. The two sets of entries are disjoint — an entry is carried
        /// precisely because the catalogue does not contain it, and AcquisitionCoordinator only ever adds ids
        /// it has checked against the catalogue — so they are concatenated without a merge.
        /// </summary>
        public static InventorySnapshot Create(Inventory inventory, EssenceWallet wallet,
            IReadOnlyList<InventorySnapshotEntry> carried)
        {
            return Build(inventory.Entries(), wallet.Balance, carried);
        }

        /// <summary>
        /// The snapshot the state WOULD have after <paramref name="change"/>. Pure: it reads the live objects
        /// and mutates neither, which is what lets the caller write before it applies. Throws if the change
        /// would produce a negative count or a negative balance — the caller is expected to have refused that
        /// already.
        /// </summary>
        public static InventorySnapshot CreateWith(Inventory inventory, EssenceWallet wallet,
            IReadOnlyList<InventorySnapshotEntry> carried, StateChange change)
        {
            List<InventoryEntry> owned = new List<InventoryEntry>(inventory.Entries());

            if (change.CopyDelta != 0)
            {
                if (!change.Relic.IsValid)
                {
                    throw new ArgumentException("A change that moves a copy needs the relic it moves.", nameof(change));
                }

                int index = IndexOf(owned, change.Relic);
                int current = index < 0 ? 0 : owned[index].Count;
                int next = current + change.CopyDelta;

                if (next < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(change), change.CopyDelta,
                        $"'{change.Relic}' is owned {current} time(s); the change would take that below zero.");
                }

                if (next == 0)
                {
                    owned.RemoveAt(index);
                }
                else if (index < 0)
                {
                    owned.Add(new InventoryEntry(change.Relic, next));
                    owned.Sort(ByIdOrdinal);
                }
                else
                {
                    owned[index] = new InventoryEntry(change.Relic, next);
                }
            }

            int balance = wallet.Balance + change.EssenceDelta;

            if (balance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(change), change.EssenceDelta,
                    $"The balance is {wallet.Balance}; the change would take it below zero.");
            }

            return Build(owned, balance, carried);
        }

        private static InventorySnapshot Build(IReadOnlyList<InventoryEntry> owned, int essence,
            IReadOnlyList<InventorySnapshotEntry> carried)
        {
            int carriedCount = carried == null ? 0 : carried.Count;

            InventorySnapshot snapshot = new InventorySnapshot
            {
                Version = SaveFormat.Current,
                Entries = new InventorySnapshotEntry[owned.Count + carriedCount],
                Essence = essence
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

        private static int IndexOf(List<InventoryEntry> owned, RelicId id)
        {
            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int ByIdOrdinal(InventoryEntry left, InventoryEntry right)
        {
            return string.CompareOrdinal(left.Id.ToString(), right.Id.ToString());
        }
    }
}
