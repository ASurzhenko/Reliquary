using System.Collections.Generic;
using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Content
{
    public sealed class RelicContentResult
    {
        public RelicContentResult(RelicCatalog catalog, RelicPresentationLibrary presentation,
            IReadOnlyList<RelicContentIssue> issues)
        {
            Catalog = catalog;
            Presentation = presentation;
            Issues = issues;
        }

        public RelicCatalog Catalog { get; }

        public RelicPresentationLibrary Presentation { get; }

        public IReadOnlyList<RelicContentIssue> Issues { get; }
    }

    public sealed class RelicContentLoader
    {
        /// <summary>Folder under a Resources/ directory that every relic asset must live in.</summary>
        public static readonly string RelicsResourceFolder = "Relics";

        public RelicContentResult Load()
        {
            return LoadFrom(Resources.LoadAll<RelicDefinition>(RelicsResourceFolder));
        }

        private RelicContentResult LoadFrom(IReadOnlyList<RelicDefinition> definitions)
        {
            List<RelicContentIssue> issues = new List<RelicContentIssue>();
            List<Relic> relics = new List<Relic>();

            // Keyed on the Relic INSTANCE, not on its id: two assets may claim one id, and only the catalogue
            // decides which of those instances survives. Relic has no value equality (see its <remarks>), so
            // each asset's entry stays distinct here.
            Dictionary<Relic, RelicPresentation> byInstance = new Dictionary<Relic, RelicPresentation>();

            for (int i = 0; i < definitions.Count; i++)
            {
                RelicDefinition definition = definitions[i];

                if (definition == null)
                {
                    issues.Add(RelicContentIssue.Warning(default, $"Entry {i} was null and was skipped."));
                    continue;
                }

                if (!definition.TryCreateRelic(out Relic relic, out string error))
                {
                    issues.Add(RelicContentIssue.Error(default, $"{definition.name}: {error}"));
                    continue;
                }

                relics.Add(relic);

                string[] summaries = new string[definition.Effects.Count];

                for (int effectIndex = 0; effectIndex < summaries.Length; effectIndex++)
                {
                    summaries[effectIndex] = definition.Effects[effectIndex].Summary;
                }

                byInstance[relic] = new RelicPresentation(definition.DisplayName, definition.Description,
                    definition.Icon, summaries);
            }

            RelicCatalog catalog = RelicCatalog.Create(relics, out IReadOnlyList<RelicContentIssue> catalogIssues);
            issues.AddRange(catalogIssues);

            Dictionary<RelicId, RelicPresentation> presentations =
                new Dictionary<RelicId, RelicPresentation>(catalog.Count);

            foreach (Relic accepted in catalog.All)
            {
                presentations[accepted.Id] = byInstance[accepted];
            }

            return new RelicContentResult(catalog, new RelicPresentationLibrary(presentations), issues);
        }
    }
}
