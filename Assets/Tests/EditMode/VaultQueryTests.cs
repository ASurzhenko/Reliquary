using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class VaultQueryTests
    {
        [Test]
        public void SparesFirst_ThenOrdinalById()
        {
            RelicCatalog catalog = Catalogue("relic.ashen_censer", "relic.drowned_bell", "relic.sunken_crown");
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.sunken_crown"));
            inventory.Add(new RelicId("relic.ashen_censer"));
            inventory.Add(new RelicId("relic.ashen_censer"));
            inventory.Add(new RelicId("relic.drowned_bell"));
            inventory.Add(new RelicId("relic.drowned_bell"));

            IReadOnlyList<InventoryEntry> ordered = VaultQuery.Order(catalog, inventory);

            Assert.That(Ids(ordered), Is.EqualTo(new[] { "relic.ashen_censer", "relic.drowned_bell", "relic.sunken_crown" }));
            Assert.That(ordered[0].Count, Is.EqualTo(2));
            Assert.That(ordered[1].Count, Is.EqualTo(2));
            Assert.That(ordered[2].Count, Is.EqualTo(1));
        }

        [Test]
        public void EqualSpares_KeepOrdinalOrder_AtAnyListLength()
        {
            // Enough entries that the sort partitions rather than insertion-sorts them: the tie-break is what
            // holds the order, and a sort is not contracted to be stable.
            string[] ids = new string[24];

            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = $"relic.{i:00}";
            }

            RelicCatalog catalog = Catalogue(ids);
            Inventory inventory = new Inventory();

            for (int i = 0; i < ids.Length; i++)
            {
                inventory.Add(new RelicId(ids[i]));
                inventory.Add(new RelicId(ids[i]));
            }

            Assert.That(Ids(VaultQuery.Order(catalog, inventory)), Is.EqualTo(ids));
        }

        [Test]
        public void SingleCopies_AreListedAfterSpares()
        {
            RelicCatalog catalog = Catalogue("relic.ashen_censer", "relic.tide_lantern");
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.ashen_censer"));
            inventory.Add(new RelicId("relic.tide_lantern"));
            inventory.Add(new RelicId("relic.tide_lantern"));
            inventory.Add(new RelicId("relic.tide_lantern"));

            IReadOnlyList<InventoryEntry> ordered = VaultQuery.Order(catalog, inventory);

            Assert.That(Ids(ordered), Is.EqualTo(new[] { "relic.tide_lantern", "relic.ashen_censer" }));
        }

        [Test]
        public void EmptyInventory_ReturnsEmpty_NotNull()
        {
            RelicCatalog catalog = Catalogue("relic.ashen_censer");

            IReadOnlyList<InventoryEntry> ordered = VaultQuery.Order(catalog, new Inventory());

            Assert.That(ordered, Is.Not.Null);
            Assert.That(ordered, Is.Empty);
        }

        [Test]
        public void IdsNotInTheCatalogue_AreOmitted()
        {
            RelicCatalog catalog = Catalogue("relic.ashen_censer");
            Inventory inventory = new Inventory(new[]
            {
                new InventoryEntry(new RelicId("relic.ashen_censer"), 1),
                new InventoryEntry(new RelicId("relic.from_a_later_build"), 4)
            });

            IReadOnlyList<InventoryEntry> ordered = VaultQuery.Order(catalog, inventory);

            Assert.That(Ids(ordered), Is.EqualTo(new[] { "relic.ashen_censer" }));
        }

        [Test]
        public void HasAnySpares_IsFalseWhenEveryRelicHasOneCopy()
        {
            Inventory single = new Inventory();
            single.Add(new RelicId("relic.ashen_censer"));
            single.Add(new RelicId("relic.tide_lantern"));

            Inventory spare = new Inventory();
            spare.Add(new RelicId("relic.ashen_censer"));
            spare.Add(new RelicId("relic.ashen_censer"));

            Assert.That(VaultQuery.HasAnySpares(new Inventory()), Is.False);
            Assert.That(VaultQuery.HasAnySpares(single), Is.False);
            Assert.That(VaultQuery.HasAnySpares(spare), Is.True);
        }

        [Test]
        public void SpareCopies_CountsEveryCopyBeyondTheFirst()
        {
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.ashen_censer"));
            inventory.Add(new RelicId("relic.ashen_censer"));
            inventory.Add(new RelicId("relic.ashen_censer"));
            inventory.Add(new RelicId("relic.tide_lantern"));
            inventory.Add(new RelicId("relic.tide_lantern"));
            inventory.Add(new RelicId("relic.sunken_crown"));

            Assert.That(VaultQuery.SpareCopies(new Inventory()), Is.EqualTo(0));
            Assert.That(VaultQuery.SpareCopies(inventory), Is.EqualTo(3));
        }

        private static RelicCatalog Catalogue(params string[] ids)
        {
            Relic[] relics = new Relic[ids.Length];

            for (int i = 0; i < ids.Length; i++)
            {
                relics[i] = new Relic(new RelicId(ids[i]), 10, 1, Array.Empty<IRelicEffect>());
            }

            return RelicCatalog.Create(relics, out _);
        }

        private static string[] Ids(IReadOnlyList<InventoryEntry> entries)
        {
            string[] ids = new string[entries.Count];

            for (int i = 0; i < entries.Count; i++)
            {
                ids[i] = entries[i].Id.ToString();
            }

            return ids;
        }
    }
}
