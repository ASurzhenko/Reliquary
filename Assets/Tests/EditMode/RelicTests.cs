using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class RelicTests
    {
        [Test]
        public void DefaultId_IsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                new Relic(default, 10, 100, Array.Empty<IRelicEffect>()));
        }

        [Test]
        public void NegativeEssenceValue_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Relic(new RelicId("relic.sunken_crown"), -1, 100, Array.Empty<IRelicEffect>()));
        }

        [Test]
        public void ZeroDiscoveryWeight_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Relic(new RelicId("relic.sunken_crown"), 10, 0, Array.Empty<IRelicEffect>()));
        }

        [Test]
        public void NullEffectEntry_IsRejected()
        {
            IRelicEffect[] effects = { new FakeEffect(), null };

            Assert.Throws<ArgumentException>(() =>
                new Relic(new RelicId("relic.sunken_crown"), 10, 100, effects));
        }

        [Test]
        public void EffectList_IsCopied()
        {
            List<IRelicEffect> effects = new List<IRelicEffect> { new FakeEffect() };
            Relic relic = new Relic(new RelicId("relic.sunken_crown"), 10, 100, effects);

            effects.Clear();

            Assert.That(relic.Effects.Count, Is.EqualTo(1));
        }
    }
}
