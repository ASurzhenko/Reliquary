using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// A relic as the rules see it: identity, the numbers the economy reads, and the behaviours it grants.
    /// What it looks like and what it is called live in the content layer.
    /// </summary>
    /// <remarks>
    /// Equals and GetHashCode are deliberately NOT overridden: reference identity is load-bearing. The
    /// content loader pairs each relic instance with the presentation data of the asset it came from by
    /// keying a dictionary on the instance, and two assets may legitimately claim one id while that
    /// collision is being reported. Value equality would collapse them into one key and re-pair a relic
    /// with the wrong asset's name and icon.
    /// </remarks>
    public sealed class Relic
    {
        public Relic(RelicId id, int essenceValue, int discoveryWeight, IReadOnlyList<IRelicEffect> effects)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("A relic needs a valid id.", nameof(id));
            }

            if (essenceValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(essenceValue), essenceValue, "Essence value cannot be negative.");
            }

            if (discoveryWeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(discoveryWeight), discoveryWeight, "Discovery weight must be positive.");
            }

            IRelicEffect[] copied = effects == null ? Array.Empty<IRelicEffect>() : new IRelicEffect[effects.Count];

            for (int i = 0; i < copied.Length; i++)
            {
                copied[i] = effects[i] ?? throw new ArgumentException($"Effect {i} is null.", nameof(effects));
            }

            Id = id;
            EssenceValue = essenceValue;
            DiscoveryWeight = discoveryWeight;
            Effects = copied;
        }

        public RelicId Id { get; }

        /// <summary>Essence a duplicate of this relic dissolves into.</summary>
        public int EssenceValue { get; }

        /// <summary>Relative weight when an acquisition picks a relic. Higher is more common.</summary>
        public int DiscoveryWeight { get; }

        public IReadOnlyList<IRelicEffect> Effects { get; }
    }
}
