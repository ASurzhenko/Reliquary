using System.Collections.Generic;
using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Content
{
    public sealed class SetContentResult
    {
        public SetContentResult(SetCatalog sets, SetPresentationLibrary presentation,
            IReadOnlyList<RelicContentIssue> issues)
        {
            Sets = sets;
            Presentation = presentation;
            Issues = issues;
        }

        public SetCatalog Sets { get; }

        public SetPresentationLibrary Presentation { get; }

        public IReadOnlyList<RelicContentIssue> Issues { get; }
    }

    public sealed class SetContentLoader
    {
        /// <summary>Folder under a Resources/ directory that every set asset must live in.</summary>
        public static readonly string SetsResourceFolder = "Sets";

        public SetContentResult Load(RelicCatalog relics)
        {
            return LoadFrom(Resources.LoadAll<SetDefinition>(SetsResourceFolder), relics);
        }

        private SetContentResult LoadFrom(IReadOnlyList<SetDefinition> definitions, RelicCatalog relics)
        {
            List<RelicContentIssue> issues = new List<RelicContentIssue>();
            List<RelicSet> sets = new List<RelicSet>();

            // Keyed on the RelicSet INSTANCE, not on its id: two assets may claim one id, and only the
            // catalogue decides which of those instances survives.
            Dictionary<RelicSet, SetPresentation> byInstance = new Dictionary<RelicSet, SetPresentation>();

            for (int i = 0; i < definitions.Count; i++)
            {
                SetDefinition definition = definitions[i];

                if (definition == null)
                {
                    issues.Add(RelicContentIssue.Warning(default, $"Set entry {i} was null and was skipped."));
                    continue;
                }

                if (!definition.TryCreateSet(out RelicSet set, out string error))
                {
                    issues.Add(RelicContentIssue.Error(default, $"{definition.name}: {error}"));
                    continue;
                }

                sets.Add(set);

                string[] summaries = new string[definition.Perks.Count];

                for (int perkIndex = 0; perkIndex < summaries.Length; perkIndex++)
                {
                    summaries[perkIndex] = definition.Perks[perkIndex].Summary;
                }

                byInstance[set] = new SetPresentation(definition.DisplayName, definition.Description,
                    summaries, DimensionsOf(set));
            }

            SetCatalog catalog = SetCatalog.Create(sets, relics, out IReadOnlyList<RelicContentIssue> catalogIssues);
            issues.AddRange(catalogIssues);

            Dictionary<SetId, SetPresentation> presentations = new Dictionary<SetId, SetPresentation>(catalog.Count);

            foreach (RelicSet accepted in catalog.All)
            {
                presentations[accepted.Id] = byInstance[accepted];
            }

            return new SetContentResult(catalog, new SetPresentationLibrary(presentations), issues);
        }

        private static ModifierDimension DimensionsOf(RelicSet set)
        {
            ModifierDimension dimensions = ModifierDimension.None;

            for (int i = 0; i < set.Perks.Count; i++)
            {
                dimensions |= ModifierDimensions.Of(set.Perks[i]);
            }

            return dimensions;
        }
    }
}
