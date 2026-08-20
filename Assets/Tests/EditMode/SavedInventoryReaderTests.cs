using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class SavedInventoryReaderTests
    {
        [Test]
        public void NullSnapshot_IsRefused()
        {
            SavedInventory saved = SavedInventoryReader.Read(null, Catalog());

            AssertRefused(saved);
        }

        [Test]
        public void ZeroVersion_IsRefused()
        {
            // The fixture carries one well-formed, catalogued entry on purpose: with no entries the refusal
            // could equally be the empty-entries rule, and this test would not show the version check firing.
            SavedInventory saved = SavedInventoryReader.Read(
                Snapshot(0, Entry("relic.sunken_crown", 1)), Catalog());

            AssertRefused(saved);
        }

        [Test]
        public void FutureVersion_IsRefused()
        {
            SavedInventory saved = SavedInventoryReader.Read(
                Snapshot(SaveFormat.Current + 1, Entry("relic.sunken_crown", 1)), Catalog());

            AssertRefused(saved);
            Assert.That(saved.Issues[0].Message, Does.Contain((SaveFormat.Current + 1).ToString()));
        }

        [Test]
        public void NullEntries_IsRefused()
        {
            AssertRefused(SavedInventoryReader.Read(
                Snapshot(SaveFormat.Current, (InventorySnapshotEntry[])null), Catalog()));
        }

        [Test]
        public void EmptyEntries_IsRefused()
        {
            AssertRefused(SavedInventoryReader.Read(
                Snapshot(SaveFormat.Current, Array.Empty<InventorySnapshotEntry>()), Catalog()));
        }

        [Test]
        public void NullEntry_IsSkippedWithAWarning()
        {
            SavedInventory saved = SavedInventoryReader.Read(
                Snapshot(SaveFormat.Current, null, Entry("relic.sunken_crown", 2)), Catalog());

            Assert.That(saved.Status, Is.EqualTo(SavedInventoryStatus.Restored));
            Assert.That(saved.Inventory.CountOf(new RelicId("relic.sunken_crown")), Is.EqualTo(2));
            AssertOneWarning(saved);
        }

        [Test]
        public void BlankId_IsSkippedWithAWarning()
        {
            SavedInventory saved = SavedInventoryReader.Read(
                Snapshot(SaveFormat.Current, Entry("   ", 2), Entry("relic.sunken_crown", 1)), Catalog());

            Assert.That(saved.Inventory.DistinctCount, Is.EqualTo(1));
            AssertOneWarning(saved);
        }

        [Test]
        public void NonPositiveCount_IsSkippedWithAWarning()
        {
            SavedInventory saved = SavedInventoryReader.Read(
                Snapshot(SaveFormat.Current, Entry("relic.sunken_crown", 0), Entry("relic.pyre_key", 1)), Catalog());

            Assert.That(saved.Inventory.Owns(new RelicId("relic.sunken_crown")), Is.False);
            Assert.That(saved.Inventory.Owns(new RelicId("relic.pyre_key")), Is.True);
            AssertOneWarning(saved);
        }

        [Test]
        public void DuplicateId_KeepsTheLargerCountAndWarns()
        {
            SavedInventory saved = SavedInventoryReader.Read(
                Snapshot(SaveFormat.Current, Entry("relic.sunken_crown", 5), Entry("relic.sunken_crown", 3)),
                Catalog());

            // First-wins would discard two copies the player owns; the non-lossy branch keeps the larger.
            Assert.That(saved.Inventory.CountOf(new RelicId("relic.sunken_crown")), Is.EqualTo(5));
            AssertOneWarning(saved);
            Assert.That(saved.Issues[0].Message, Does.Contain("5"));
            Assert.That(saved.Issues[0].Message, Does.Contain("3"));
        }

        [Test]
        public void UnknownRelic_IsCarriedNotDropped()
        {
            SavedInventory saved = SavedInventoryReader.Read(
                Snapshot(SaveFormat.Current, Entry("relic.sunken_crown", 1), Entry("relic.not_in_this_build", 3)),
                Catalog());

            Assert.That(saved.Inventory.Owns(new RelicId("relic.not_in_this_build")), Is.False);
            Assert.That(saved.Carried.Count, Is.EqualTo(1), "dropping it deletes the player's copies on the next save");
            Assert.That(saved.Carried[0].RelicId, Is.EqualTo("relic.not_in_this_build"));
            Assert.That(saved.Carried[0].Count, Is.EqualTo(3));
            AssertOneWarning(saved);
        }

        [Test]
        public void PaddedId_MatchesTheCatalogue()
        {
            SavedInventory saved = SavedInventoryReader.Read(
                Snapshot(SaveFormat.Current, Entry("  relic.sunken_crown  ", 2)), Catalog());

            Assert.That(saved.Inventory.CountOf(new RelicId("relic.sunken_crown")), Is.EqualTo(2));
            Assert.That(saved.Carried, Is.Empty, "without the trim a good entry falls into the carried branch");
        }

        [Test]
        public void WellFormed_RestoresEveryCount()
        {
            SavedInventory saved = SavedInventoryReader.Read(
                Snapshot(SaveFormat.Current, Entry("relic.sunken_crown", 2), Entry("relic.pyre_key", 1)), Catalog());

            Assert.That(saved.Status, Is.EqualTo(SavedInventoryStatus.Restored));
            Assert.That(saved.Inventory.CountOf(new RelicId("relic.sunken_crown")), Is.EqualTo(2));
            Assert.That(saved.Inventory.CountOf(new RelicId("relic.pyre_key")), Is.EqualTo(1));
            Assert.That(saved.Issues, Is.Empty);
        }

        private static void AssertRefused(SavedInventory saved)
        {
            Assert.That(saved.Status, Is.EqualTo(SavedInventoryStatus.Refused));
            Assert.That(saved.Inventory.DistinctCount, Is.EqualTo(0));
            Assert.That(saved.Carried, Is.Empty);
            Assert.That(saved.Issues.Count, Is.EqualTo(1));
            Assert.That(saved.Issues[0].Severity, Is.EqualTo(RelicContentSeverity.Error));
        }

        private static void AssertOneWarning(SavedInventory saved)
        {
            Assert.That(saved.Issues.Count, Is.EqualTo(1));
            Assert.That(saved.Issues[0].Severity, Is.EqualTo(RelicContentSeverity.Warning));
        }

        private static RelicCatalog Catalog()
        {
            return RelicCatalog.Create(new List<Relic>
            {
                new Relic(new RelicId("relic.sunken_crown"), 10, 100, Array.Empty<IRelicEffect>()),
                new Relic(new RelicId("relic.pyre_key"), 10, 100, Array.Empty<IRelicEffect>())
            }, out _);
        }

        private static InventorySnapshot Snapshot(int version, params InventorySnapshotEntry[] entries)
        {
            return new InventorySnapshot { Version = version, Entries = entries };
        }

        private static InventorySnapshotEntry Entry(string id, int count)
        {
            return new InventorySnapshotEntry { RelicId = id, Count = count };
        }
    }
}
