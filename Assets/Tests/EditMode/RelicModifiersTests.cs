using System;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class RelicModifiersTests
    {
        [Test]
        public void NoEffects_LeaveMultiplierAtOne()
        {
            RelicModifiers modifiers = RelicModifiers.From(Array.Empty<IRelicEffect>());

            Assert.That(modifiers.EssenceMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.UnownedPullBonus, Is.EqualTo(0));
        }

        [Test]
        public void Contributions_AreOrderIndependent()
        {
            IRelicEffect small = new FakeEffect(0.05f);
            IRelicEffect medium = new FakeEffect(0.15f);
            IRelicEffect large = new FakeEffect(0.4f);

            RelicModifiers forwards = RelicModifiers.From(new[] { small, medium, large });
            RelicModifiers backwards = RelicModifiers.From(new[] { large, medium, small });

            // float addition is not associative, so this is equal within a tolerance rather than exactly.
            Assert.That(backwards.EssenceMultiplier,
                Is.EqualTo(forwards.EssenceMultiplier).Within(1e-5f));
            Assert.That(forwards.EssenceMultiplier, Is.EqualTo(1.6f).Within(1e-5f));
        }

        [Test]
        public void EssenceMultiplier_ClampsAtZero()
        {
            RelicModifiers modifiers = RelicModifiers.From(
                new IRelicEffect[] { new FakeEffect(-0.5f), new FakeEffect(-0.5f), new FakeEffect(-0.5f) });

            Assert.That(modifiers.EssenceMultiplier, Is.EqualTo(0f));
        }

        [Test]
        public void UnownedPullBonus_Accumulates()
        {
            RelicModifiers modifiers = RelicModifiers.From(
                new IRelicEffect[] { new FakeEffect(0f, 15), new FakeEffect(0f, 25) });

            Assert.That(modifiers.UnownedPullBonus, Is.EqualTo(40));
        }

        [Test]
        public void UnownedPullBonus_ClampsAtZero()
        {
            RelicModifiers modifiers = RelicModifiers.From(
                new IRelicEffect[] { new FakeEffect(0f, 10), new FakeEffect(0f, -25) });

            Assert.That(modifiers.UnownedPullBonus, Is.EqualTo(0));
        }
    }
}
