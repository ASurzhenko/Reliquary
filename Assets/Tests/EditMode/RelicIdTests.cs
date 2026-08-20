using System;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class RelicIdTests
    {
        [Test]
        public void SameValue_AreEqual()
        {
            RelicId first = new RelicId("relic.sunken_crown");
            RelicId second = new RelicId("relic.sunken_crown");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first == second, Is.True);
        }

        [Test]
        public void DifferentCase_AreNotEqual()
        {
            RelicId lower = new RelicId("relic.sunken_crown");
            RelicId mixed = new RelicId("Relic.Sunken_Crown");

            Assert.That(lower, Is.Not.EqualTo(mixed));
            Assert.That(lower != mixed, Is.True);
        }

        [Test]
        public void Default_IsNotValid()
        {
            Assert.That(default(RelicId).IsValid, Is.False);
        }

        [Test]
        public void BlankValue_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new RelicId("   "));
        }
    }
}
