using System;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class DrawTableTests
    {
        [Test]
        public void RollsMapToWeightBands()
        {
            RelicCatalog catalog = RelicCatalog.Create(new[]
            {
                Make("relic.a", 30),
                Make("relic.b", 70)
            }, out _);

            DrawTable table = DrawTable.Build(catalog, new Inventory(), new RelicModifiers());

            Assert.That(table.TotalWeight, Is.EqualTo(100));
            Assert.That(table.Pick(0).ToString(), Is.EqualTo("relic.a"));
            Assert.That(table.Pick(29).ToString(), Is.EqualTo("relic.a"));
            Assert.That(table.Pick(30).ToString(), Is.EqualTo("relic.b"));
            Assert.That(table.Pick(table.TotalWeight - 1).ToString(), Is.EqualTo("relic.b"));
        }

        [Test]
        public void RollOutsideRange_IsRejected()
        {
            DrawTable table = DrawTable.Build(RelicCatalog.Create(new[] { Make("relic.a", 10) }, out _),
                new Inventory(), new RelicModifiers());

            Assert.Throws<ArgumentOutOfRangeException>(() => table.Pick(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.Pick(table.TotalWeight));
        }

        [Test]
        public void UnownedPullBonus_AppliesOnlyToUnowned()
        {
            RelicCatalog catalog = RelicCatalog.Create(new[]
            {
                Make("relic.a", 10),
                Make("relic.b", 10)
            }, out _);

            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.a"));

            RelicModifiers modifiers = new RelicModifiers();
            modifiers.AddUnownedPullBonus(30);

            DrawTable table = DrawTable.Build(catalog, inventory, modifiers);

            // 10 for the owned relic, 40 for the one still missing.
            Assert.That(table.TotalWeight, Is.EqualTo(50));
            Assert.That(table.Pick(9).ToString(), Is.EqualTo("relic.a"));
            Assert.That(table.Pick(10).ToString(), Is.EqualTo("relic.b"));
        }

        [Test]
        public void EmptyCatalogue_HasZeroTotalWeight()
        {
            DrawTable table = DrawTable.Build(RelicCatalog.Create(Array.Empty<Relic>(), out _),
                new Inventory(), new RelicModifiers());

            // This is what makes the Rejected terminal reachable rather than theoretical.
            Assert.That(table.TotalWeight, Is.EqualTo(0));
        }

        private static Relic Make(string id, int discoveryWeight)
        {
            return new Relic(new RelicId(id), 10, discoveryWeight, Array.Empty<IRelicEffect>());
        }
    }
}
