using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class InventoryTests
    {
        [Test]
        public void FirstCopy_IsOwnedOnce()
        {
            Inventory inventory = new Inventory();

            InventoryChange change = inventory.Add(new RelicId("relic.sunken_crown"));

            Assert.That(change.WasFirstCopy, Is.True);
            Assert.That(change.Count, Is.EqualTo(1));
            Assert.That(inventory.Owns(new RelicId("relic.sunken_crown")), Is.True);
            Assert.That(inventory.DistinctCount, Is.EqualTo(1));
        }

        [Test]
        public void SecondCopy_IncrementsCount()
        {
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.sunken_crown"));

            InventoryChange change = inventory.Add(new RelicId("relic.sunken_crown"));

            Assert.That(change.WasFirstCopy, Is.False);
            Assert.That(change.Count, Is.EqualTo(2));
            Assert.That(inventory.CountOf(new RelicId("relic.sunken_crown")), Is.EqualTo(2));
            Assert.That(inventory.DistinctCount, Is.EqualTo(1), "a duplicate is a count, not a second kind of relic");
        }

        [Test]
        public void Add_RaisesChangedOncePerCall()
        {
            Inventory inventory = new Inventory();
            List<InventoryChange> raised = new List<InventoryChange>();
            inventory.Changed += change => raised.Add(change);

            inventory.Add(new RelicId("relic.sunken_crown"));
            inventory.Add(new RelicId("relic.sunken_crown"));

            // Persistence rides this event: a doubled raise doubles every save.
            Assert.That(raised.Count, Is.EqualTo(2));
            Assert.That(raised[1].Count, Is.EqualTo(2));
        }

        [Test]
        public void UnknownId_CountsZeroAndIsNotOwned()
        {
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.sunken_crown"));

            Assert.That(inventory.Owns(new RelicId("relic.pyre_key")), Is.False);
            Assert.That(inventory.CountOf(new RelicId("relic.pyre_key")), Is.EqualTo(0));
        }

        [Test]
        public void Entries_AreOrdinalById()
        {
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.tide_lantern"));
            inventory.Add(new RelicId("relic.ashen_censer"));
            inventory.Add(new RelicId("relic.pyre_key"));

            IReadOnlyList<InventoryEntry> entries = inventory.Entries();

            Assert.That(entries[0].Id.ToString(), Is.EqualTo("relic.ashen_censer"));
            Assert.That(entries[1].Id.ToString(), Is.EqualTo("relic.pyre_key"));
            Assert.That(entries[2].Id.ToString(), Is.EqualTo("relic.tide_lantern"));
        }

        [Test]
        public void RestoreWithNonPositiveCount_IsRejected()
        {
            InventoryEntry[] restored = { new InventoryEntry(new RelicId("relic.sunken_crown"), 0) };

            Assert.Throws<ArgumentOutOfRangeException>(() => new Inventory(restored));
        }

        [Test]
        public void RestoreWithDuplicateId_IsRejected()
        {
            InventoryEntry[] restored =
            {
                new InventoryEntry(new RelicId("relic.sunken_crown"), 1),
                new InventoryEntry(new RelicId("relic.sunken_crown"), 2)
            };

            Assert.Throws<ArgumentException>(() => new Inventory(restored));
        }
    }
}
