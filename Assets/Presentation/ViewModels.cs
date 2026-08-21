using System.Collections.Generic;
using UnityEngine;
using Reliquary.Content;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>How a relic draws. One token, produced from one value, read by the views.</summary>
    public enum RelicTileState
    {
        Unfound,
        Owned,
        Duplicate
    }

    public readonly struct RelicTileModel
    {
        public RelicTileModel(string name, Sprite icon, int copies, RelicTileState state)
        {
            Name = name;
            Icon = icon;
            Copies = copies;
            State = state;
        }

        public string Name { get; }

        public Sprite Icon { get; }

        public int Copies { get; }

        public RelicTileState State { get; }
    }

    public readonly struct VaultRowModel
    {
        public VaultRowModel(string name, Sprite icon, int copies, bool canDissolve, int yield,
            DissolveOutcome refusal)
        {
            Name = name;
            Icon = icon;
            Copies = copies;
            CanDissolve = canDissolve;
            Yield = yield;
            Refusal = refusal;
        }

        public string Name { get; }

        public Sprite Icon { get; }

        public int Copies { get; }

        /// <summary>The domain's token. The row never counts copies to decide whether it may offer this.</summary>
        public bool CanDissolve { get; }

        /// <summary>What the dissolve would pay at the current modifiers. The number on the button.</summary>
        public int Yield { get; }

        /// <summary>Why not, when it cannot.</summary>
        public DissolveOutcome Refusal { get; }
    }

    public readonly struct SetSectionModel
    {
        public SetSectionModel(string name, string counter, float fraction, bool isComplete, string perkLine,
            bool tracksProgress)
        {
            Name = name;
            Counter = counter;
            Fraction = fraction;
            IsComplete = isComplete;
            PerkLine = perkLine;
            TracksProgress = tracksProgress;
        }

        public string Name { get; }

        /// <summary>"2 / 4", or the copy a set nobody has started deserves.</summary>
        public string Counter { get; }

        /// <summary>The domain's own division. No view divides two domain values.</summary>
        public float Fraction { get; }

        public bool IsComplete { get; }

        /// <summary>What completing this grants, said before it is complete.</summary>
        public string PerkLine { get; }

        /// <summary>
        /// False for the section that holds relics no set names. There is no goal to show a bar or a perk
        /// for, and pretending otherwise would invent progress the domain never reported.
        /// </summary>
        public bool TracksProgress { get; }
    }

    public readonly struct RelicDetailModel
    {
        public RelicDetailModel(string name, string description, Sprite icon, string ownership, int essenceValue,
            int discoveryWeight, IReadOnlyList<string> effectSummaries, IReadOnlyList<string> setLines)
        {
            Name = name;
            Description = description;
            Icon = icon;
            Ownership = ownership;
            EssenceValue = essenceValue;
            DiscoveryWeight = discoveryWeight;
            EffectSummaries = effectSummaries;
            SetLines = setLines;
        }

        public string Name { get; }

        public string Description { get; }

        public Sprite Icon { get; }

        public string Ownership { get; }

        public int EssenceValue { get; }

        public int DiscoveryWeight { get; }

        public IReadOnlyList<string> EffectSummaries { get; }

        /// <summary>One line per set this relic belongs to, with that set's progress. Empty for a loose relic.</summary>
        public IReadOnlyList<string> SetLines { get; }
    }

    /// <summary>
    /// Turns what the domain and the content layer say into what a row draws. The only decision made here is
    /// which drawing token a copy count maps to; every rule the screens branch on comes from the domain, and
    /// this file is where the mapping and the wording live so they live in exactly one place.
    /// </summary>
    public static class ViewModels
    {
        /// <summary>The heading of the section that holds relics no set names.</summary>
        public readonly static string LooseSectionName = "UNBOUND RELICS";

        public static RelicTileModel Tile(RelicId id, RelicPresentationLibrary presentation, int copies)
        {
            RelicPresentation content = Resolve(id, presentation, out string name);

            return new RelicTileModel(name, content.Icon, copies, StateOf(copies));
        }

        public static VaultRowModel VaultRow(RelicId id, RelicPresentationLibrary presentation, int copies,
            DissolvePreview preview)
        {
            RelicPresentation content = Resolve(id, presentation, out string name);

            return new VaultRowModel(name, content.Icon, copies, preview.CanDissolve, preview.Yield,
                preview.Refusal);
        }

        public static SetSectionModel Section(RelicSet set, SetPresentationLibrary presentation,
            SetProgress progress)
        {
            SetPresentation content = ResolveSet(set.Id, presentation, out string name);

            return new SetSectionModel(name, CounterOf(progress), progress.Fraction, progress.IsComplete,
                PerkLineOf(name, content, progress), true);
        }

        /// <summary>The section for relics no set names: a heading, and nothing that claims to be a goal.</summary>
        public static SetSectionModel LooseSection(int count)
        {
            return new SetSectionModel(LooseSectionName, $"{count} in no set", 0f, false,
                "These belong to no set yet.", false);
        }

        public static RelicDetailModel Detail(Relic relic, RelicPresentationLibrary presentation, int copies,
            SetCatalog sets, SetPresentationLibrary setPresentation, Inventory inventory)
        {
            RelicPresentation content = Resolve(relic.Id, presentation, out string name);

            return new RelicDetailModel(name, content.Description, content.Icon, OwnershipOf(copies),
                relic.EssenceValue, relic.DiscoveryWeight, content.EffectSummaries,
                SetLinesFor(relic.Id, sets, setPresentation, inventory));
        }

        /// <summary>The name a set is known by, for a view that has an id and needs a sentence.</summary>
        public static string SetName(SetId id, SetPresentationLibrary presentation)
        {
            ResolveSet(id, presentation, out string name);
            return name;
        }

        /// <summary>
        /// What a completed set grants, in the perk asset's own words. Empty when the set grants nothing —
        /// which the content loader already reported as a warning.
        /// </summary>
        public static string PerkSummary(SetId id, SetPresentationLibrary presentation)
        {
            SetPresentation content = ResolveSet(id, presentation, out _);

            return Join(content.PerkSummaries);
        }

        /// <summary>Which outputs a set's perks move, so a view can flash the numbers that changed.</summary>
        public static ModifierDimension DimensionsOf(SetId id, SetPresentationLibrary presentation)
        {
            SetPresentation content = ResolveSet(id, presentation, out _);

            return content.Dimensions;
        }

        private static RelicTileState StateOf(int copies)
        {
            if (copies == 0)
            {
                return RelicTileState.Unfound;
            }

            return copies == 1 ? RelicTileState.Owned : RelicTileState.Duplicate;
        }

        private static string OwnershipOf(int copies)
        {
            if (copies == 0)
            {
                return "Not yet found";
            }

            return copies == 1 ? "Owned — one copy" : $"Owned — {copies} copies";
        }

        private static string CounterOf(SetProgress progress)
        {
            // IsUnstarted is the domain's token, not an Owned == 0 the view worked out for itself.
            return progress.IsUnstarted
                ? $"Not yet begun — 0 / {progress.Total}"
                : $"{progress.Owned} / {progress.Total}";
        }

        private static string PerkLineOf(string name, SetPresentation content, SetProgress progress)
        {
            string summary = Join(content.PerkSummaries);

            if (string.IsNullOrEmpty(summary))
            {
                return "Completing this set grants nothing yet.";
            }

            return progress.IsComplete
                ? $"{name} is complete: {summary}"
                : $"Completing {name} grants: {summary}";
        }

        private static IReadOnlyList<string> SetLinesFor(RelicId relic, SetCatalog sets,
            SetPresentationLibrary presentation, Inventory inventory)
        {
            if (sets == null || inventory == null)
            {
                return System.Array.Empty<string>();
            }

            IReadOnlyList<RelicSet> holders = sets.SetsContaining(relic);
            string[] lines = new string[holders.Count];

            for (int i = 0; i < holders.Count; i++)
            {
                SetProgress progress = SetProgress.For(holders[i], inventory);
                ResolveSet(holders[i].Id, presentation, out string name);

                lines[i] = $"{name} — {CounterOf(progress)}";
            }

            return lines;
        }

        private static string Join(IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                return string.Empty;
            }

            if (lines.Count == 1)
            {
                return lines[0];
            }

            return string.Join("  ·  ", lines);
        }

        private static RelicPresentation Resolve(RelicId id, RelicPresentationLibrary presentation, out string name)
        {
            // A relic carried in from a save this build no longer describes has no name or icon of its own,
            // so the id is the honest fallback rather than an empty row.
            if (presentation != null && presentation.TryGet(id, out RelicPresentation found))
            {
                name = found.DisplayName;
                return found;
            }

            name = id.ToString();
            return default;
        }

        private static SetPresentation ResolveSet(SetId id, SetPresentationLibrary presentation, out string name)
        {
            if (presentation != null && presentation.TryGet(id, out SetPresentation found)
                && !string.IsNullOrWhiteSpace(found.DisplayName))
            {
                name = found.DisplayName;
                return found;
            }

            name = id.IsValid ? id.ToString() : "Unknown set";
            return default;
        }
    }
}
