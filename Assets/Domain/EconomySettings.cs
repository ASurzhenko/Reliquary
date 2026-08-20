using System;

namespace Reliquary.Domain
{
    /// <summary>
    /// The two numbers the trader prices with. Authored as content; guarded here because a multiplier the
    /// Inspector cannot produce still reaches this constructor from a hand-edited or badly merged asset.
    /// </summary>
    public sealed class EconomySettings
    {
        private readonly float _priceMultiplier;
        private readonly int _priceFloor;

        /// <summary>
        /// Throws on a multiplier below 1 or a floor below 1. A multiplier under 1 would make every relic
        /// cheaper to buy than its own duplicate is worth, which makes duplicates worthless — the opposite of
        /// this mechanic. The content validator catches this at author time; this is the last line.
        /// </summary>
        public EconomySettings(float priceMultiplier, int priceFloor)
        {
            if (priceMultiplier < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(priceMultiplier), priceMultiplier,
                    "A price multiplier below 1 makes a relic cheaper to buy than its own duplicate is worth.");
            }

            if (priceFloor < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(priceFloor), priceFloor,
                    "A price floor of 0 would make the cheapest relics free.");
            }

            _priceMultiplier = priceMultiplier;
            _priceFloor = priceFloor;
        }

        public float PriceMultiplier => _priceMultiplier;

        public int PriceFloor => _priceFloor;

        /// <summary>What the trader charges for this relic. Floor, so a price is never below the floor.</summary>
        public int PriceOf(Relic relic)
        {
            if (relic == null)
            {
                throw new ArgumentNullException(nameof(relic));
            }

            int priced = (int)Math.Floor(relic.EssenceValue * _priceMultiplier);
            return priced < _priceFloor ? _priceFloor : priced;
        }
    }
}
