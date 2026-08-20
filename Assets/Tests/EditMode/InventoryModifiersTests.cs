using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class InventoryModifiersTests
    {
        [Test]
        public void OwnedEffects_AccumulateAcrossRelics()
        {
            RelicCatalog catalog = RelicCatalog.Create(new[]
            {
                Make("relic.sunken_crown", new FakeEffect(0.25f, 10)),
                Make("relic.pyre_key", new FakeEffect(0.25f, 5))
            }, out _);

            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.sunken_crown"));
            inventory.Add(new RelicId("relic.pyre_key"));

            RelicModifiers modifiers = InventoryModifiers.From(catalog, inventory);

            Assert.That(modifiers.EssenceMultiplier, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(modifiers.UnownedPullBonus, Is.EqualTo(15));
        }

        [Test]
        public void DuplicateCopies_DoNotStackModifiers()
        {
            RelicCatalog catalog = RelicCatalog.Create(new[]
            {
                Make("relic.sunken_crown", new FakeEffect(0.25f, 10))
            }, out _);

            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.sunken_crown"));
            inventory.Add(new RelicId("relic.sunken_crown"));
            inventory.Add(new RelicId("relic.sunken_crown"));

            RelicModifiers modifiers = InventoryModifiers.From(catalog, inventory);

            // A duplicate is worth essence; letting it also multiply an effect pays for the same copy twice.
            Assert.That(modifiers.EssenceMultiplier, Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(modifiers.UnownedPullBonus, Is.EqualTo(10));
        }

        private static Relic Make(string id, params IRelicEffect[] effects)
        {
            return new Relic(new RelicId(id), 10, 100, effects);
        }
    }
}
