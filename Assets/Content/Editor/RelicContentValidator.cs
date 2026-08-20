using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Reliquary.Domain;

namespace Reliquary.Content.Editor
{
    /// <summary>
    /// Checks every relic asset in the project and says, in one sentence a designer can act on, what is wrong
    /// with it. Console lines carry the asset as context, so clicking one selects the file.
    /// </summary>
    internal sealed class RelicContentValidator : AssetPostprocessor
    {
        private static readonly string EffectsFieldName = "_effects";
        private static readonly string ContentFolder = "Assets/Content/";
        private static readonly string AssetExtension = ".asset";

        [MenuItem("Tools/Reliquary/Validate Relic Content")]
        private static void ValidateFromMenu()
        {
            RelicContentReport report = Validate();

            string resolved = report.ResolvedCount < 0
                ? "the catalogue could not be loaded"
                : $"{report.ResolvedCount} relics resolved";

            EditorUtility.DisplayDialog("Relic content",
                report.ErrorCount == 0
                    ? $"{resolved}, {report.WarningCount} warning(s), no errors."
                    : $"{report.ErrorCount} error(s) in relic content. See the Console — click a line to select the asset.",
                "OK");
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            // A cold import of a clean clone brings every asset in at once, so a sweep fired mid-batch would
            // report icons that simply have not been imported yet. The menu item is always available.
            if (didDomainReload || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!TouchesRelicContent(importedAssets) && !TouchesRelicContent(deletedAssets)
                && !TouchesRelicContent(movedAssets))
            {
                return;
            }

            Validate();
        }

        private static bool TouchesRelicContent(string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];

                if (path.StartsWith(ContentFolder, StringComparison.Ordinal)
                    && path.EndsWith(AssetExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static RelicContentReport Validate()
        {
            RelicContentReport report = new RelicContentReport();

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(RelicDefinition)}");

            // Path of the first asset seen for each id, so a duplicate can name both sides.
            Dictionary<string, string> firstPathById = new Dictionary<string, string>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                RelicDefinition definition = AssetDatabase.LoadAssetAtPath<RelicDefinition>(path);

                if (definition == null)
                {
                    Error(report, "Sweep", $"'{path}' matched a relic search but could not be loaded. " +
                        "The file is damaged or its script reference is missing.", null);
                    continue;
                }

                try
                {
                    Inspect(definition, path, firstPathById, report);
                }
                catch (Exception exception)
                {
                    // One damaged asset must not stop the other seven from being checked.
                    Error(report, "Sweep", $"'{path}' could not be checked: {exception.Message}", definition);
                }
            }

            Debug.Log($"{nameof(RelicContentValidator)}.{nameof(Validate)} [Sweep] " +
                $"Checked {guids.Length} relic asset(s) in the project.");

            return Resolve(report);
        }

        private static void Inspect(RelicDefinition definition, string path,
            Dictionary<string, string> firstPathById, RelicContentReport report)
        {
            string name = System.IO.Path.GetFileName(path);

            // Rule 1 — the asset has to sit where Resources.LoadAll will find it. The folder comes from the
            // loader's own constant, so a rename cannot leave the two checking different places.
            string requiredFolder = $"/Resources/{RelicContentLoader.RelicsResourceFolder}/";

            if (path.IndexOf(requiredFolder, StringComparison.Ordinal) < 0)
            {
                Error(report, "Folder", $"'{name}' is at {path} — it will not appear in the catalogue. " +
                    $"Move it under a Resources/{RelicContentLoader.RelicsResourceFolder} folder.", definition);
            }

            string id = definition.TrimmedId;

            if (id.Length == 0)
            {
                // Rule 2
                Error(report, "Id", $"'{name}' has no id. Give it a stable id such as 'relic.sunken_crown'.",
                    definition);
            }
            else if (firstPathById.TryGetValue(id, out string firstPath))
            {
                // Rule 3 — both paths, because only the editor has them.
                Error(report, "Duplicate", $"'{firstPath}' and '{path}' both use the id '{id}'. " +
                    "Ids are compared after trimming, so check for leading or trailing spaces. " +
                    "Ids must be unique — rename one.", definition);
            }
            else
            {
                firstPathById.Add(id, path);
            }

            // Rule 4
            if (definition.Icon == null)
            {
                Error(report, "Icon", $"'{name}' has no icon. Assign a sprite from Assets/Content/Icons.",
                    definition);
            }

            InspectEffects(definition, name, report);

            // Rule 7 — [Min] is an Inspector attribute, not a deserialization clamp, so a hand-edited or
            // badly-merged asset walks a bad value straight through import.
            if (definition.EssenceValue < 0)
            {
                Error(report, "Numbers", $"'{name}' has an essence value of {definition.EssenceValue}. " +
                    "The Inspector cannot produce that, so this asset was edited by hand or merged badly. " +
                    "Essence must be 0 or more and discovery weight 1 or more.", definition);
            }

            if (definition.DiscoveryWeight < 1)
            {
                Error(report, "Numbers", $"'{name}' has a discovery weight of {definition.DiscoveryWeight}. " +
                    "The Inspector cannot produce that, so this asset was edited by hand or merged badly. " +
                    "Essence must be 0 or more and discovery weight 1 or more.", definition);
            }

            // Rule 8
            if (string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                Warning(report, "Name", $"'{name}' has no display name — the Collection will show an empty row.",
                    definition);
            }

            if (string.IsNullOrWhiteSpace(definition.Description))
            {
                Warning(report, "Name", $"'{name}' has no description — the Collection will show an empty row.",
                    definition);
            }
        }

        private static void InspectEffects(RelicDefinition definition, string name, RelicContentReport report)
        {
            SerializedObject serialized = new SerializedObject(definition);
            SerializedProperty effects = serialized.FindProperty(EffectsFieldName);

            for (int i = 0; i < effects.arraySize; i++)
            {
                SerializedProperty slot = effects.GetArrayElementAtIndex(i);

                if (slot.objectReferenceValue != null)
                {
                    continue;
                }

                // A non-zero instance id with a null reference is a reference to something that used to
                // exist. Telling a designer which of the two happened is the whole point.
                if (slot.objectReferenceInstanceIDValue != 0)
                {
                    // Rule 6
                    Error(report, "Effect", $"'{name}' effect slot {i} points at an asset that no longer " +
                        "exists (broken reference).", definition);
                }
                else
                {
                    // Rule 5
                    Error(report, "Effect", $"'{name}' effect slot {i} is empty. " +
                        "Assign an effect asset or remove the slot.", definition);
                }
            }
        }

        /// <summary>
        /// Runs the load the game itself runs at boot and reports what came back. This observes; it does not
        /// gate. The sweep above is where refusal lives — this is here so a wrong Resources path shows up as
        /// a resolved count that disagrees with the swept count, in one glance.
        /// </summary>
        private static RelicContentReport Resolve(RelicContentReport report)
        {
            RelicContentResult resolved;

            try
            {
                resolved = new RelicContentLoader().Load();
            }
            catch (Exception exception)
            {
                report.ResolvedCount = -1;
                Error(report, "Resolve", "Loading the catalogue threw, so the shipping path could not be " +
                    $"checked: {exception.Message}. A relic asset holds a value the Inspector cannot " +
                    "produce — the sweep above names it.", null);
                return report;
            }

            report.ResolvedCount = resolved.Catalog.Count;

            foreach (RelicContentIssue issue in resolved.Issues)
            {
                if (issue.Severity == RelicContentSeverity.Error)
                {
                    Error(report, "Resolve", issue.Message, null);
                }
                else
                {
                    Warning(report, "Resolve", issue.Message, null);
                }
            }

            List<string> resolvedIds = new List<string>(resolved.Catalog.Count);

            foreach (Relic relic in resolved.Catalog.All)
            {
                resolvedIds.Add(relic.Id.ToString());
            }

            Debug.Log($"{nameof(RelicContentValidator)}.{nameof(Validate)} [Resolve] " +
                $"Resources resolved {report.ResolvedCount} relics: {string.Join(", ", resolvedIds)}");

            return report;
        }

        private static void Error(RelicContentReport report, string tag, string message, UnityEngine.Object context)
        {
            report.ErrorCount++;
            Debug.LogError($"{nameof(RelicContentValidator)}.{nameof(Validate)} [{tag}] {message}", context);
        }

        private static void Warning(RelicContentReport report, string tag, string message, UnityEngine.Object context)
        {
            report.WarningCount++;
            Debug.LogWarning($"{nameof(RelicContentValidator)}.{nameof(Validate)} [{tag}] {message}", context);
        }

        private sealed class RelicContentReport
        {
            public int ErrorCount { get; set; }

            public int WarningCount { get; set; }

            /// <summary>Relics the shipping Resources load returned, or -1 when that load threw.</summary>
            public int ResolvedCount { get; set; }
        }
    }
}
