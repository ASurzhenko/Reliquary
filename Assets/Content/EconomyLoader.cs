using System;
using System.Collections.Generic;
using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Content
{
    public sealed class EconomyLoader
    {
        /// <summary>Folder under a Resources/ directory the economy asset must live in.</summary>
        public static readonly string EconomyResourceFolder = "Economy";

        /// <summary>
        /// Used when the asset is missing or holds numbers the rules refuse. The game still runs and the
        /// console says why the numbers came from code rather than from content.
        /// </summary>
        public static readonly float FallbackPriceMultiplier = 3f;

        public static readonly int FallbackPriceFloor = 10;

        public EconomySettings Load(out IReadOnlyList<RelicContentIssue> issues)
        {
            return LoadFrom(Resources.LoadAll<EconomyDefinition>(EconomyResourceFolder), out issues);
        }

        private EconomySettings LoadFrom(IReadOnlyList<EconomyDefinition> definitions,
            out IReadOnlyList<RelicContentIssue> issues)
        {
            List<RelicContentIssue> found = new List<RelicContentIssue>();
            issues = found;

            EconomyDefinition definition = FirstOf(definitions, found);

            if (definition == null)
            {
                found.Add(RelicContentIssue.Error(default,
                    $"No economy asset was found under Resources/{EconomyResourceFolder}. " +
                    $"Prices fall back to a multiplier of {FallbackPriceMultiplier} and a floor of {FallbackPriceFloor}."));
                return Fallback();
            }

            try
            {
                return new EconomySettings(definition.PriceMultiplier, definition.PriceFloor);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                // [Range] and [Min] are Inspector attributes, not deserialization clamps, so a hand-edited or
                // badly merged asset walks a bad value straight through import.
                found.Add(RelicContentIssue.Error(default,
                    $"'{definition.name}' holds numbers the rules refuse: {exception.Message} " +
                    $"Prices fall back to a multiplier of {FallbackPriceMultiplier} and a floor of {FallbackPriceFloor}."));
                return Fallback();
            }
        }

        private static EconomyDefinition FirstOf(IReadOnlyList<EconomyDefinition> definitions,
            List<RelicContentIssue> issues)
        {
            EconomyDefinition first = null;

            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] == null)
                {
                    continue;
                }

                if (first == null)
                {
                    first = definitions[i];
                    continue;
                }

                issues.Add(RelicContentIssue.Error(default,
                    $"More than one economy asset exists; '{first.name}' was used and '{definitions[i].name}' " +
                    "was ignored. Which one wins is not something content should decide by accident."));
            }

            return first;
        }

        private static EconomySettings Fallback()
        {
            return new EconomySettings(FallbackPriceMultiplier, FallbackPriceFloor);
        }
    }
}
