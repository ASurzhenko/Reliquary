using System;
using NUnit.Framework;
using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    /// <summary>
    /// The real decoder over payloads this code did not write in this run. What JsonUtility returns for a
    /// missing field, an empty array and broken bytes is recorded here rather than assumed, because the read
    /// path's refusals rest on it. The payloads are the same literals the editor save tools write.
    /// </summary>
    public class SavedInventoryCodecTests
    {
        /// <summary>
        /// Captured from a real run: three acquisitions in play mode, then the value of the PlayerPrefs key
        /// read back byte for byte. Serialising a snapshot here and deserialising it again would prove the
        /// round trip, not that this is the shape the game actually writes.
        /// </summary>
        private static readonly string CapturedSave =
            "{\"Version\":1,\"Entries\":[{\"RelicId\":\"relic.cinder_mask\",\"Count\":1}," +
            "{\"RelicId\":\"relic.coral_signet\",\"Count\":1},{\"RelicId\":\"relic.drowned_bell\",\"Count\":1}]}";

        [Test]
        public void CapturedSave_DecodesToTheCountsItWasWrittenFrom()
        {
            InventorySnapshot snapshot = JsonUtility.FromJson<InventorySnapshot>(CapturedSave);

            SavedInventory saved = SavedInventoryReader.Read(snapshot, FullCatalog());

            Assert.That(snapshot.Version, Is.EqualTo(SaveFormat.Current));
            Assert.That(saved.Status, Is.EqualTo(SavedInventoryStatus.Restored));
            Assert.That(saved.Inventory.DistinctCount, Is.EqualTo(3));
            Assert.That(saved.Inventory.CountOf(new RelicId("relic.cinder_mask")), Is.EqualTo(1));
            Assert.That(saved.Inventory.CountOf(new RelicId("relic.coral_signet")), Is.EqualTo(1));
            Assert.That(saved.Inventory.CountOf(new RelicId("relic.drowned_bell")), Is.EqualTo(1));
            Assert.That(saved.Carried, Is.Empty, "every id in a save this build wrote is in this build's catalogue");
            Assert.That(saved.Issues, Is.Empty);
        }

        [Test]
        public void MissingEntriesField_ReachesRuleFour()
        {
            InventorySnapshot snapshot = JsonUtility.FromJson<InventorySnapshot>("{\"Version\":1}");

            SavedInventory saved = SavedInventoryReader.Read(snapshot, Catalog());

            Assert.That(saved.Status, Is.EqualTo(SavedInventoryStatus.Refused));
            Assert.That(saved.Issues[0].Message, Does.Contain("no state"));
        }

        [Test]
        public void EmptyEntriesArray_ReachesRuleFour()
        {
            InventorySnapshot snapshot = JsonUtility.FromJson<InventorySnapshot>("{\"Version\":1,\"Entries\":[]}");

            SavedInventory saved = SavedInventoryReader.Read(snapshot, Catalog());

            Assert.That(saved.Status, Is.EqualTo(SavedInventoryStatus.Refused));
            Assert.That(saved.Issues[0].Message, Does.Contain("no state"));
        }

        [Test]
        public void MissingVersionField_DecodesToZero()
        {
            InventorySnapshot snapshot = JsonUtility.FromJson<InventorySnapshot>(
                "{\"Entries\":[{\"RelicId\":\"relic.sunken_crown\",\"Count\":1}]}");

            // A silent-default decode leaves a value field at its default, which is the premise rules 2 and
            // 4 rest on: no !=null test could see it.
            Assert.That(snapshot.Version, Is.EqualTo(0));
            Assert.That(SavedInventoryReader.Read(snapshot, Catalog()).Status,
                Is.EqualTo(SavedInventoryStatus.Refused));
        }

        [Test]
        public void BrokenJson_ThrowsArgumentException()
        {
            // The store catches exactly this, which is why it has to be the exception the codec really throws.
            Assert.Throws<ArgumentException>(() =>
                JsonUtility.FromJson<InventorySnapshot>("{\"Version\":1,\"Entries\":[{\"RelicId\":"));
        }

        [Test]
        public void UnknownField_IsIgnored()
        {
            // The field name has to be one the shape does NOT declare, or this stops testing anything while
            // still passing: it read "Essence" until the save shape grew an Essence field.
            InventorySnapshot snapshot = JsonUtility.FromJson<InventorySnapshot>(
                "{\"Version\":1,\"Wallet\":40,\"Entries\":[{\"RelicId\":\"relic.sunken_crown\",\"Count\":2}]}");

            // Adding a field to the save shape therefore needs no version bump: an older payload decodes
            // with the new field defaulted, and a newer one decodes with the unknown field dropped.
            Assert.That(snapshot.Version, Is.EqualTo(1));
            Assert.That(snapshot.Entries.Length, Is.EqualTo(1));
            Assert.That(snapshot.Entries[0].Count, Is.EqualTo(2));
        }

        [Test]
        public void P3EraPayload_DecodesWithZeroEssence()
        {
            // The captured payload was written before the save shape carried a balance, so it is the real
            // evidence that an older save decodes with the new field defaulted rather than refused.
            InventorySnapshot snapshot = JsonUtility.FromJson<InventorySnapshot>(CapturedSave);

            SavedInventory saved = SavedInventoryReader.Read(snapshot, FullCatalog());

            Assert.That(snapshot.Essence, Is.EqualTo(0));
            Assert.That(saved.Status, Is.EqualTo(SavedInventoryStatus.Restored));
            Assert.That(saved.Essence, Is.EqualTo(0));
            Assert.That(saved.Inventory.DistinctCount, Is.EqualTo(3), "no relic is lost to the added field");
            Assert.That(saved.Issues, Is.Empty, "an older save is not a complaint");
        }

        [Test]
        public void NullEntriesWithEssence_RestoresTheBalance()
        {
            // JsonUtility leaves an absent array member null, so this payload reaches the reader with
            // Entries == null and a balance worth keeping. Before the normalisation it threw at the loop.
            InventorySnapshot snapshot = JsonUtility.FromJson<InventorySnapshot>("{\"Version\":1,\"Essence\":100}");

            Assert.That(snapshot.Entries, Is.Null);

            SavedInventory saved = SavedInventoryReader.Read(snapshot, Catalog());

            Assert.That(saved.Status, Is.EqualTo(SavedInventoryStatus.Restored));
            Assert.That(saved.Inventory.DistinctCount, Is.EqualTo(0));
            Assert.That(saved.Essence, Is.EqualTo(100));
            Assert.That(saved.Issues, Is.Empty);
        }

        private static RelicCatalog Catalog()
        {
            return RelicCatalog.Create(new[]
            {
                new Relic(new RelicId("relic.sunken_crown"), 10, 100, Array.Empty<IRelicEffect>())
            }, out _);
        }

        private static RelicCatalog FullCatalog()
        {
            return RelicCatalog.Create(new[]
            {
                new Relic(new RelicId("relic.cinder_mask"), 10, 100, Array.Empty<IRelicEffect>()),
                new Relic(new RelicId("relic.coral_signet"), 10, 100, Array.Empty<IRelicEffect>()),
                new Relic(new RelicId("relic.drowned_bell"), 10, 100, Array.Empty<IRelicEffect>())
            }, out _);
        }
    }
}
