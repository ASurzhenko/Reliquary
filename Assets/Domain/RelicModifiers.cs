using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// What every active effect adds up to. Contributions are additive, so applying the same set of effects
    /// in any order yields the same values.
    /// </summary>
    public sealed class RelicModifiers
    {
        private float _essenceBonus;
        private int _unownedPullBonus;

        /// <summary>Multiplies the essence a duplicate dissolves into. 1 when nothing is granted.</summary>
        public float EssenceMultiplier => _essenceBonus <= -1f ? 0f : 1f + _essenceBonus;

        /// <summary>Extra acquisition weight given to relics the player does not own yet. 0 by default.</summary>
        public int UnownedPullBonus => _unownedPullBonus < 0 ? 0 : _unownedPullBonus;

        public void AddEssenceBonus(float fraction)
        {
            _essenceBonus += fraction;
        }

        public void AddUnownedPullBonus(int weight)
        {
            _unownedPullBonus += weight;
        }

        public static RelicModifiers From(IEnumerable<IRelicEffect> effects)
        {
            RelicModifiers modifiers = new RelicModifiers();

            if (effects != null)
            {
                foreach (IRelicEffect effect in effects)
                {
                    effect?.Apply(modifiers);
                }
            }

            return modifiers;
        }
    }
}
