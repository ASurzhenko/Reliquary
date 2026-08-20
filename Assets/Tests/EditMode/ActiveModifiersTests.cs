using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    /// <summary>
    /// What is contributing right now: the effects of the relics owned, plus the perks of the sets
    /// completed. One accumulator for both, which is the whole claim — a set perk is not a parallel system.
    /// </summary>
    public class ActiveModifiersTests
    {
        [Test]
        public void OwnedRelicEffects_Accumulate()
        {
            RelicCatalog relics = RelicCatalog.Create(new[]
            {
                MakeRelic("relic.sunken_crown", new FakeEffect(0.25f, 10)),
                MakeRelic("relic.pyre_key", new FakeEffect(0.25f, 5))
            }, out _);

            Inventory inventory = Owning("relic.sunken_crown", "relic.pyre_key");

            RelicModifiers modifiers = ActiveModifiers.For(relics, NoSets(relics), inventory);

            Assert.That(modifiers.EssenceMultiplier, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(modifiers.UnownedPullBonus, Is.EqualTo(15));
        }

        [Test]
        public void DuplicateCopies_DoNotStackModifiers()
        {
            RelicCatalog relics = RelicCatalog.Create(new[]
            {
                MakeRelic("relic.sunken_crown", new FakeEffect(0.25f, 10))
            }, out _);

            Inventory inventory = Owning("relic.sunken_crown", "relic.sunken_crown", "relic.sunken_crown");

            RelicModifiers modifiers = ActiveModifiers.For(relics, NoSets(relics), inventory);

            // A duplicate is worth essence; letting it also multiply an effect pays for the same copy twice.
            Assert.That(modifiers.EssenceMultiplier, Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(modifiers.UnownedPullBonus, Is.EqualTo(10));
        }

        [Test]
        public void CompleteSetPerk_IsIncluded()
        {
            RelicCatalog relics = TwoRelics();
            SetCatalog sets = Sets(relics, Set("set.tideworn", new FakeEffect(0f, 150), "relic.a", "relic.b"));

            RelicModifiers modifiers = ActiveModifiers.For(relics, sets, Owning("relic.a", "relic.b"));

            // The perk reaches the same accumulator a relic effect reaches, through the same call.
            Assert.That(modifiers.UnownedPullBonus, Is.EqualTo(150));
        }

        [Test]
        public void IncompleteSetPerk_IsNotIncluded()
        {
            RelicCatalog relics = TwoRelics();
            SetCatalog sets = Sets(relics, Set("set.tideworn", new FakeEffect(0f, 150), "relic.a", "relic.b"));

            RelicModifiers modifiers = ActiveModifiers.For(relics, sets, Owning("relic.a"));

            Assert.That(modifiers.UnownedPullBonus, Is.EqualTo(0), "without this the perk is free");
        }

        [Test]
        public void TwoCompleteSets_Sum()
        {
            RelicCatalog relics = RelicCatalog.Create(new[]
            {
                MakeRelic("relic.a"), MakeRelic("relic.b"), MakeRelic("relic.c"), MakeRelic("relic.d")
            }, out _);

            SetCatalog sets = Sets(relics,
                Set("set.tideworn", new FakeEffect(0f, 150), "relic.a", "relic.b"),
                Set("set.emberwrought", new FakeEffect(0.25f), "relic.c", "relic.d"));

            RelicModifiers modifiers = ActiveModifiers.For(relics, sets,
                Owning("relic.a", "relic.b", "relic.c", "relic.d"));

            Assert.That(modifiers.UnownedPullBonus, Is.EqualTo(150));
            Assert.That(modifiers.EssenceMultiplier, Is.EqualTo(1.25f).Within(0.0001f));
        }

        [Test]
        public void AboveTheRail_IsCappedAndSaysSo()
        {
            RelicCatalog relics = TwoRelics();
            SetCatalog sets = Sets(relics,
                Set("set.a", new FakeEffect(4f, 900), "relic.a", "relic.b"));

            RelicModifiers modifiers = ActiveModifiers.For(relics, sets, Owning("relic.a", "relic.b"));

            Assert.That(modifiers.EssenceMultiplier, Is.EqualTo(RelicModifiers.MaxEssenceMultiplier));
            Assert.That(modifiers.UnownedPullBonus, Is.EqualTo(RelicModifiers.MaxUnownedPullBonus));

            // The flag reads the same raw expression the clamped getter does, so the two cannot disagree.
            Assert.That(modifiers.EssenceMultiplierWasCapped, Is.True);
            Assert.That(modifiers.UnownedPullBonusWasCapped, Is.True);
        }

        private static Inventory Owning(params string[] ids)
        {
            Inventory inventory = new Inventory();

            for (int i = 0; i < ids.Length; i++)
            {
                inventory.Add(new RelicId(ids[i]));
            }

            return inventory;
        }

        private static RelicCatalog TwoRelics()
        {
            return RelicCatalog.Create(new[] { MakeRelic("relic.a"), MakeRelic("relic.b") }, out _);
        }

        private static Relic MakeRelic(string id, params IRelicEffect[] effects)
        {
            return new Relic(new RelicId(id), 10, 100, effects);
        }

        private static RelicSet Set(string id, IRelicEffect perk, params string[] members)
        {
            List<RelicId> ids = new List<RelicId>(members.Length);

            for (int i = 0; i < members.Length; i++)
            {
                ids.Add(new RelicId(members[i]));
            }

            return new RelicSet(new SetId(id), ids, new[] { perk });
        }

        private static SetCatalog Sets(RelicCatalog relics, params RelicSet[] sets)
        {
            return SetCatalog.Create(sets, relics, out _);
        }

        private static SetCatalog NoSets(RelicCatalog relics)
        {
            return SetCatalog.Create(Array.Empty<RelicSet>(), relics, out _);
        }
    }
}
