using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class CollectionQueryTests
    {
        [Test]
        public void All_ReturnsEveryCatalogueRelic()
        {
            RelicCatalog catalog = Catalogue("relic.ashen_censer", "relic.drowned_bell", "relic.sunken_crown");
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.drowned_bell"));

            IReadOnlyList<Relic> shown = CollectionQuery.Filter(catalog, inventory, CollectionFilter.All);

            Assert.That(Ids(shown), Is.EqualTo(new[] { "relic.ashen_censer", "relic.drowned_bell", "relic.sunken_crown" }));
        }

        [Test]
        public void Owned_ReturnsOnlyOwned()
        {
            RelicCatalog catalog = Catalogue("relic.ashen_censer", "relic.drowned_bell", "relic.sunken_crown");
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.drowned_bell"));
            inventory.Add(new RelicId("relic.sunken_crown"));

            IReadOnlyList<Relic> shown = CollectionQuery.Filter(catalog, inventory, CollectionFilter.Owned);

            Assert.That(Ids(shown), Is.EqualTo(new[] { "relic.drowned_bell", "relic.sunken_crown" }));
        }

        [Test]
        public void Missing_IsTheExactComplementOfOwned()
        {
            RelicCatalog catalog = Catalogue("relic.ashen_censer", "relic.drowned_bell", "relic.sunken_crown");
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.drowned_bell"));

            IReadOnlyList<Relic> owned = CollectionQuery.Filter(catalog, inventory, CollectionFilter.Owned);
            IReadOnlyList<Relic> missing = CollectionQuery.Filter(catalog, inventory, CollectionFilter.Missing);

            List<string> together = new List<string>(Ids(owned));
            together.AddRange(Ids(missing));
            together.Sort(StringComparer.Ordinal);

            Assert.That(together, Is.EqualTo(new[] { "relic.ashen_censer", "relic.drowned_bell", "relic.sunken_crown" }));
            Assert.That(Ids(missing), Has.No.Member("relic.drowned_bell"));
        }

        [Test]
        public void Order_IsTheCatalogueOrder_ForEveryFilter()
        {
            RelicCatalog catalog = Catalogue("relic.ashen_censer", "relic.drowned_bell", "relic.sunken_crown", "relic.tide_lantern");
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.tide_lantern"));
            inventory.Add(new RelicId("relic.ashen_censer"));

            Assert.That(Ids(CollectionQuery.Filter(catalog, inventory, CollectionFilter.All)),
                Is.EqualTo(Ids(catalog.All)));
            Assert.That(Ids(CollectionQuery.Filter(catalog, inventory, CollectionFilter.Owned)),
                Is.EqualTo(new[] { "relic.ashen_censer", "relic.tide_lantern" }));
            Assert.That(Ids(CollectionQuery.Filter(catalog, inventory, CollectionFilter.Missing)),
                Is.EqualTo(new[] { "relic.drowned_bell", "relic.sunken_crown" }));
        }

        [Test]
        public void EmptyInventory_MissingReturnsEverything()
        {
            RelicCatalog catalog = Catalogue("relic.ashen_censer", "relic.drowned_bell");
            Inventory inventory = new Inventory();

            Assert.That(Ids(CollectionQuery.Filter(catalog, inventory, CollectionFilter.Missing)),
                Is.EqualTo(new[] { "relic.ashen_censer", "relic.drowned_bell" }));
            Assert.That(CollectionQuery.Filter(catalog, inventory, CollectionFilter.Owned), Is.Empty);
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

        private static string[] Ids(IReadOnlyList<Relic> relics)
        {
            string[] ids = new string[relics.Count];

            for (int i = 0; i < relics.Count; i++)
            {
                ids[i] = relics[i].Id.ToString();
            }

            return ids;
        }
    }
}
