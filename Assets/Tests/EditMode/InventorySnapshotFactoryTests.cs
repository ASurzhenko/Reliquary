using System;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class InventorySnapshotFactoryTests
    {
        [Test]
        public void RoundTrip_PreservesCounts()
        {
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.sunken_crown"));
            inventory.Add(new RelicId("relic.sunken_crown"));
            inventory.Add(new RelicId("relic.pyre_key"));

            InventorySnapshot snapshot = InventorySnapshotFactory.Create(inventory,
                Array.Empty<InventorySnapshotEntry>());
            SavedInventory read = SavedInventoryReader.Read(snapshot, Catalog());

            Assert.That(read.Status, Is.EqualTo(SavedInventoryStatus.Restored));
            Assert.That(read.Inventory.CountOf(new RelicId("relic.sunken_crown")), Is.EqualTo(2));
            Assert.That(read.Inventory.CountOf(new RelicId("relic.pyre_key")), Is.EqualTo(1));
        }

        [Test]
        public void CarriedEntries_SurviveASaveTheyWereNotPartOf()
        {
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.sunken_crown"));

            InventorySnapshotEntry[] carried =
            {
                new InventorySnapshotEntry { RelicId = "relic.not_in_this_build", Count = 3 }
            };

            InventorySnapshot snapshot = InventorySnapshotFactory.Create(inventory, carried);

            Assert.That(snapshot.Entries.Length, Is.EqualTo(2), "the carried entry is written back, not merely reported");
            Assert.That(snapshot.Entries[1].RelicId, Is.EqualTo("relic.not_in_this_build"));
            Assert.That(snapshot.Entries[1].Count, Is.EqualTo(3));
        }

        [Test]
        public void Snapshot_CarriesTheCurrentVersion()
        {
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.sunken_crown"));

            InventorySnapshot snapshot = InventorySnapshotFactory.Create(inventory,
                Array.Empty<InventorySnapshotEntry>());

            Assert.That(snapshot.Version, Is.EqualTo(SaveFormat.Current));
            Assert.That(snapshot.Version, Is.GreaterThan(0), "a version of 0 is what the reader refuses");
        }

        private static RelicCatalog Catalog()
        {
            return RelicCatalog.Create(new[]
            {
                new Relic(new RelicId("relic.sunken_crown"), 10, 100, Array.Empty<IRelicEffect>()),
                new Relic(new RelicId("relic.pyre_key"), 10, 100, Array.Empty<IRelicEffect>())
            }, out _);
        }
    }
}
