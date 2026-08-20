using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// What every active effect adds up to. Contributions are additive, so applying the same set of effects
    /// in any order yields the same values.
    /// </summary>
    public sealed class RelicModifiers
    {
        /// <summary>
        /// The most a duplicate may ever be worth. A safety rail against authored content that adds up
        /// further than anyone intended — not a balance dial; the balance numbers are the per-effect
        /// fractions in the content assets. Public so the content validator compares against this number
        /// rather than against a second copy of it.
        /// </summary>
        public static readonly float MaxEssenceMultiplier = 3f;

        /// <summary>
        /// The most extra weight unfound relics may ever carry. At 500 against a catalogue whose own weights
        /// total 540, unowned relics already dominate the draw.
        /// </summary>
        public static readonly int MaxUnownedPullBonus = 500;

        private float _essenceBonus;
        private int _unownedPullBonus;

        private float RawEssenceMultiplier => _essenceBonus <= -1f ? 0f : 1f + _essenceBonus;

        private int RawUnownedPullBonus => _unownedPullBonus < 0 ? 0 : _unownedPullBonus;

        /// <summary>Multiplies the essence a duplicate dissolves into. 1 when nothing is granted.</summary>
        public float EssenceMultiplier => Math.Min(RawEssenceMultiplier, MaxEssenceMultiplier);

        /// <summary>Extra acquisition weight given to relics the player does not own yet. 0 by default.</summary>
        public int UnownedPullBonus => Math.Min(RawUnownedPullBonus, MaxUnownedPullBonus);

        /// <summary>
        /// True when the rail engaged. A bound that binds without a sound is a system behaving correctly for
        /// the wrong reason, so the composition root says so once and the validator warns at author time.
        /// </summary>
        public bool EssenceMultiplierWasCapped => RawEssenceMultiplier > MaxEssenceMultiplier;

        public bool UnownedPullBonusWasCapped => RawUnownedPullBonus > MaxUnownedPullBonus;

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
