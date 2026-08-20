using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Reliquary.Domain;

namespace Reliquary.Content.Editor
{
    /// <summary>
    /// Checks every relic, set and economy asset in the project and says, in one sentence a designer can act
    /// on, what is wrong with it. Console lines carry the asset as context, so clicking one selects the file.
    /// </summary>
    internal sealed class RelicContentValidator : AssetPostprocessor
    {
        private static readonly string EffectsFieldName = "_effects";
        private static readonly string MembersFieldName = "_members";
        private static readonly string PerksFieldName = "_perks";
        private static readonly string ContentFolder = "Assets/Content/";
        private static readonly string AssetExtension = ".asset";

        [MenuItem("Tools/Reliquary/Validate Content")]
        private static void ValidateFromMenu()
        {
            RelicContentReport report = Validate();

            string resolved = report.ResolvedCount < 0
                ? "the catalogue could not be loaded"
                : $"{report.ResolvedCount} relics and {report.ResolvedSetCount} sets resolved";

            EditorUtility.DisplayDialog("Reliquary content",
                report.ErrorCount == 0
                    ? $"{resolved}, {report.WarningCount} warning(s), no errors."
                    : $"{report.ErrorCount} error(s) in content. See the Console — click a line to select the asset.",
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

            string[] setGuids = AssetDatabase.FindAssets($"t:{nameof(SetDefinition)}");
            Dictionary<string, string> firstSetPathById = new Dictionary<string, string>(setGuids.Length);

            for (int i = 0; i < setGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(setGuids[i]);
                SetDefinition definition = AssetDatabase.LoadAssetAtPath<SetDefinition>(path);

                if (definition == null)
                {
                    Error(report, "Sweep", $"'{path}' matched a set search but could not be loaded. " +
                        "The file is damaged or its script reference is missing.", null);
                    continue;
                }

                try
                {
                    InspectSet(definition, path, firstSetPathById, report);
                }
                catch (Exception exception)
                {
                    Error(report, "Sweep", $"'{path}' could not be checked: {exception.Message}", definition);
                }
            }

            Debug.Log($"{nameof(RelicContentValidator)}.{nameof(Validate)} [Sweep] " +
                $"Checked {guids.Length} relic asset(s) and {setGuids.Length} set asset(s) in the project.");

            InspectEconomy(report);
            AuditBalance(report);

            return Resolve(report);
        }

        private static void Inspect(RelicDefinition definition, string path,
            Dictionary<string, string> firstPathById, RelicContentReport report)
        {
            string name = System.IO.Path.GetFileName(path);

            // Rule 1 — the asset has to sit where Resources.LoadAll will find it. The folder comes from the
            // loader's own constant, so a rename cannot leave the two checking different places.
            if (!SitsIn(path, RelicContentLoader.RelicsResourceFolder))
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

            InspectSlots(definition, EffectsFieldName, "Effect", "effect slot", name, report);

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

        private static void InspectSet(SetDefinition definition, string path,
            Dictionary<string, string> firstPathById, RelicContentReport report)
        {
            string name = System.IO.Path.GetFileName(path);

            if (!SitsIn(path, SetContentLoader.SetsResourceFolder))
            {
                Error(report, "Folder", $"'{name}' is at {path} — it will not appear in the set catalogue. " +
                    $"Move it under a Resources/{SetContentLoader.SetsResourceFolder} folder.", definition);
            }

            string id = definition.TrimmedId;

            if (id.Length == 0)
            {
                Error(report, "Id", $"'{name}' has no id. Give it a stable id such as 'set.tideworn'.", definition);
            }
            else if (firstPathById.TryGetValue(id, out string firstPath))
            {
                Error(report, "Id", $"'{firstPath}' and '{path}' both use the id '{id}'. " +
                    "Ids must be unique — rename one.", definition);
            }
            else
            {
                firstPathById.Add(id, path);
            }

            if (definition.Members.Count == 0)
            {
                Error(report, "Members", $"'{name}' lists no members. A set with nothing in it cannot be " +
                    "completed, so it is dropped at load.", definition);
            }

            InspectSlots(definition, MembersFieldName, "Members", "member slot", name, report);
            InspectSlots(definition, PerksFieldName, "Perk", "perk slot", name, report);
            InspectMembers(definition, name, report);

            if (definition.Perks.Count == 0)
            {
                Warning(report, "Perk", $"'{name}' grants no perk. Progress is still tracked and completing " +
                    "it still announces, but nothing is granted.", definition);
            }

            if (string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                Warning(report, "Name", $"'{name}' has no display name — the Collection will show an empty " +
                    "section header.", definition);
            }
        }

        private static void InspectMembers(SetDefinition definition, string name, RelicContentReport report)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < definition.Members.Count; i++)
            {
                RelicDefinition member = definition.Members[i];

                if (member == null)
                {
                    continue;                       // already reported by the slot sweep
                }

                string memberId = member.TrimmedId;

                if (memberId.Length == 0)
                {
                    Error(report, "Members", $"'{name}' member slot {i} points at '{member.name}', which has " +
                        "no id.", definition);
                    continue;
                }

                if (!seen.Add(memberId))
                {
                    Error(report, "Members", $"'{name}' lists '{memberId}' twice. A member counts once, so " +
                        "the second slot only inflates the total.", definition);
                }

                // The orphan rule: a member the catalogue cannot load is kept at runtime and makes the set
                // uncompletable, which is honest but is not something to ship.
                string memberPath = AssetDatabase.GetAssetPath(member);

                if (!SitsIn(memberPath, RelicContentLoader.RelicsResourceFolder))
                {
                    Error(report, "Orphan", $"'{name}' lists '{memberId}' at {memberPath}, which is not where " +
                        $"the catalogue loads from. The set would keep the member and become impossible to " +
                        $"complete. Move the relic under a Resources/{RelicContentLoader.RelicsResourceFolder} " +
                        "folder or drop it from the set.", definition);
                }
            }
        }

        private static void InspectEconomy(RelicContentReport report)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(EconomyDefinition)}");

            if (guids.Length == 0)
            {
                Error(report, "Economy", "No economy asset exists. The trader falls back to a multiplier of " +
                    $"{EconomyLoader.FallbackPriceMultiplier} and a floor of {EconomyLoader.FallbackPriceFloor}. " +
                    $"Create one under a Resources/{EconomyLoader.EconomyResourceFolder} folder.", null);
                return;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EconomyDefinition definition = AssetDatabase.LoadAssetAtPath<EconomyDefinition>(path);

                if (definition == null)
                {
                    Error(report, "Economy", $"'{path}' matched an economy search but could not be loaded.", null);
                    continue;
                }

                if (i > 0)
                {
                    Error(report, "Economy", $"'{path}' is a second economy asset. Which one the game reads " +
                        "is not something content should decide by accident — delete one.", definition);
                }

                if (!SitsIn(path, EconomyLoader.EconomyResourceFolder))
                {
                    Error(report, "Economy", $"'{path}' is not under a Resources/" +
                        $"{EconomyLoader.EconomyResourceFolder} folder, so the game will not read it.", definition);
                }

                if (definition.PriceMultiplier < 1f)
                {
                    Error(report, "Economy", $"'{path}' prices at ×{definition.PriceMultiplier}. Below 1 every " +
                        "relic is cheaper to buy than its own duplicate is worth, which makes duplicates " +
                        "worthless — the opposite of this mechanic.", definition);
                }

                if (definition.PriceFloor < 1)
                {
                    Error(report, "Economy", $"'{path}' has a price floor of {definition.PriceFloor}, which " +
                        "would make the cheapest relics free.", definition);
                }
            }
        }

        /// <summary>
        /// Adds up every authored contribution — one per relic effect, one per set perk — and says so when
        /// the total could reach a rail. The rails are read off RelicModifiers rather than re-typed here, so
        /// the number the validator compares against is the number the runtime clamps with.
        /// </summary>
        private static void AuditBalance(RelicContentReport report)
        {
            RelicModifiers total = new RelicModifiers();

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(RelicDefinition)}"))
            {
                RelicDefinition relic =
                    AssetDatabase.LoadAssetAtPath<RelicDefinition>(AssetDatabase.GUIDToAssetPath(guid));

                if (relic == null)
                {
                    continue;
                }

                Apply(relic.Effects, total);
            }

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(SetDefinition)}"))
            {
                SetDefinition set =
                    AssetDatabase.LoadAssetAtPath<SetDefinition>(AssetDatabase.GUIDToAssetPath(guid));

                if (set == null)
                {
                    continue;
                }

                Apply(set.Perks, total);
            }

            if (total.EssenceMultiplierWasCapped)
            {
                Warning(report, "Balance", "Every authored essence bonus at once would push the multiplier " +
                    $"past its rail of {RelicModifiers.MaxEssenceMultiplier}, so part of the content would do " +
                    "nothing. The rail is a safety bound; the balance numbers are the per-effect fractions.", null);
            }

            if (total.UnownedPullBonusWasCapped)
            {
                Warning(report, "Balance", "Every authored pull bonus at once would push the bonus past its " +
                    $"rail of {RelicModifiers.MaxUnownedPullBonus}, so part of the content would do nothing.", null);
            }
        }

        private static void Apply(IReadOnlyList<RelicEffectDefinition> definitions, RelicModifiers total)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                definitions[i]?.CreateEffect()?.Apply(total);
            }
        }

        private static void InspectSlots(ScriptableObject definition, string fieldName, string tag,
            string label, string name, RelicContentReport report)
        {
            SerializedObject serialized = new SerializedObject(definition);
            SerializedProperty slots = serialized.FindProperty(fieldName);

            for (int i = 0; i < slots.arraySize; i++)
            {
                SerializedProperty slot = slots.GetArrayElementAtIndex(i);

                if (slot.objectReferenceValue != null)
                {
                    continue;
                }

                // A non-zero instance id with a null reference is a reference to something that used to
                // exist. Telling a designer which of the two happened is the whole point.
                if (slot.objectReferenceInstanceIDValue != 0)
                {
                    Error(report, tag, $"'{name}' {label} {i} points at an asset that no longer exists " +
                        "(broken reference).", definition);
                }
                else
                {
                    Error(report, tag, $"'{name}' {label} {i} is empty. Assign an asset or remove the slot.",
                        definition);
                }
            }
        }

        private static bool SitsIn(string path, string resourceFolder)
        {
            return path.IndexOf($"/Resources/{resourceFolder}/", StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Runs the load the game itself runs at boot and reports what came back. This observes; it does not
        /// gate. The sweep above is where refusal lives — this is here so a wrong Resources path shows up as
        /// a resolved count that disagrees with the swept count, in one glance.
        /// </summary>
        private static RelicContentReport Resolve(RelicContentReport report)
        {
            RelicContentResult resolved;
            SetContentResult resolvedSets;

            try
            {
                resolved = new RelicContentLoader().Load();
                resolvedSets = new SetContentLoader().Load(resolved.Catalog);
            }
            catch (Exception exception)
            {
                report.ResolvedCount = -1;
                Error(report, "Resolve", "Loading the catalogue threw, so the shipping path could not be " +
                    $"checked: {exception.Message}. An asset holds a value the Inspector cannot " +
                    "produce — the sweep above names it.", null);
                return report;
            }

            report.ResolvedCount = resolved.Catalog.Count;
            report.ResolvedSetCount = resolvedSets.Sets.Count;

            ReportIssues(report, resolved.Issues);
            ReportIssues(report, resolvedSets.Issues);

            List<string> resolvedIds = new List<string>(resolved.Catalog.Count);

            foreach (Relic relic in resolved.Catalog.All)
            {
                resolvedIds.Add(relic.Id.ToString());
            }

            List<string> resolvedSetIds = new List<string>(resolvedSets.Sets.Count);

            foreach (RelicSet set in resolvedSets.Sets.All)
            {
                resolvedSetIds.Add(set.Id.ToString());
            }

            Debug.Log($"{nameof(RelicContentValidator)}.{nameof(Validate)} [Resolve] " +
                $"Resources resolved {report.ResolvedCount} relics: {string.Join(", ", resolvedIds)}");
            Debug.Log($"{nameof(RelicContentValidator)}.{nameof(Validate)} [Resolve] " +
                $"Resources resolved {report.ResolvedSetCount} sets: {string.Join(", ", resolvedSetIds)}");

            return report;
        }

        private static void ReportIssues(RelicContentReport report, IReadOnlyList<RelicContentIssue> issues)
        {
            foreach (RelicContentIssue issue in issues)
            {
                if (issue.Severity == RelicContentSeverity.Error)
                {
                    Error(report, "Resolve", issue.Message, null);
                    continue;
                }

                Warning(report, "Resolve", issue.Message, null);
            }
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

            public int ResolvedSetCount { get; set; }
        }
    }
}
