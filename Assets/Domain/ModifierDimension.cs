using System;

namespace Reliquary.Domain
{
    /// <summary>Which outputs of the shared accumulator an effect moves.</summary>
    [Flags]
    public enum ModifierDimension
    {
        None = 0,
        EssenceYield = 1,
        UnownedPull = 2
    }

    public static class ModifierDimensions
    {
        /// <summary>
        /// Which outputs this effect moves, found by applying it to an empty accumulator and comparing
        /// against an untouched one. Safe because an effect only ever contributes to the accumulator — the
        /// same contract that makes application order irrelevant.
        ///
        /// The accumulators MUST be allocated fresh on every call. A cached static one would be readonly and
        /// therefore invisible to the static-state test, while contributions accumulated across probes.
        ///
        /// Three limits. An effect authored at zero reports None, because it moves nothing. An effect
        /// contributing a negative pull bonus also reports None, because the pull bonus clamps its low side
        /// and an empty accumulator already reads 0. And adding a new dimension to RelicModifiers is a core
        /// change by definition — the accumulator gains a field and this is one of the two places that
        /// change lands. Adding a new BEHAVIOUR on an existing dimension stays one new file.
        /// </summary>
        public static ModifierDimension Of(IRelicEffect effect)
        {
            if (effect == null)
            {
                return ModifierDimension.None;
            }

            RelicModifiers untouched = new RelicModifiers();
            RelicModifiers probed = new RelicModifiers();
            effect.Apply(probed);

            ModifierDimension dimensions = ModifierDimension.None;

            if (probed.EssenceMultiplier != untouched.EssenceMultiplier)
            {
                dimensions |= ModifierDimension.EssenceYield;
            }

            if (probed.UnownedPullBonus != untouched.UnownedPullBonus)
            {
                dimensions |= ModifierDimension.UnownedPull;
            }

            return dimensions;
        }
    }
}
