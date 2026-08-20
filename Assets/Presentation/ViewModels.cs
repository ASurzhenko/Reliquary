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
        public VaultRowModel(string name, Sprite icon, int copies)
        {
            Name = name;
            Icon = icon;
            Copies = copies;
        }

        public string Name { get; }

        public Sprite Icon { get; }

        public int Copies { get; }
    }

    public readonly struct RelicDetailModel
    {
        public RelicDetailModel(string name, string description, Sprite icon, string ownership, int essenceValue,
            int discoveryWeight, IReadOnlyList<string> effectSummaries)
        {
            Name = name;
            Description = description;
            Icon = icon;
            Ownership = ownership;
            EssenceValue = essenceValue;
            DiscoveryWeight = discoveryWeight;
            EffectSummaries = effectSummaries;
        }

        public string Name { get; }

        public string Description { get; }

        public Sprite Icon { get; }

        public string Ownership { get; }

        public int EssenceValue { get; }

        public int DiscoveryWeight { get; }

        public IReadOnlyList<string> EffectSummaries { get; }
    }

    /// <summary>
    /// Turns what the domain and the content layer say about a relic into what a row draws. The only decision
    /// made here is which drawing token a copy count maps to; every rule the screens branch on comes from the
    /// domain, and this file is where the mapping lives so it lives in exactly one place.
    /// </summary>
    public static class ViewModels
    {
        public static RelicTileModel Tile(RelicId id, RelicPresentationLibrary presentation, int copies)
        {
            RelicPresentation content = Resolve(id, presentation, out string name);

            return new RelicTileModel(name, content.Icon, copies, StateOf(copies));
        }

        public static VaultRowModel VaultRow(RelicId id, RelicPresentationLibrary presentation, int copies)
        {
            RelicPresentation content = Resolve(id, presentation, out string name);

            return new VaultRowModel(name, content.Icon, copies);
        }

        public static RelicDetailModel Detail(Relic relic, RelicPresentationLibrary presentation, int copies)
        {
            RelicPresentation content = Resolve(relic.Id, presentation, out string name);

            return new RelicDetailModel(name, content.Description, content.Icon, OwnershipOf(copies),
                relic.EssenceValue, relic.DiscoveryWeight, content.EffectSummaries);
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
    }
}
