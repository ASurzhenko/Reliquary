using System.Collections.Generic;
using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Content
{
    /// <summary>Everything a view needs to draw a relic, resolved by id.</summary>
    public readonly struct RelicPresentation
    {
        public RelicPresentation(string displayName, string description, Sprite icon, IReadOnlyList<string> effectSummaries)
        {
            DisplayName = displayName;
            Description = description;
            Icon = icon;
            EffectSummaries = effectSummaries;
        }

        public string DisplayName { get; }

        public string Description { get; }

        public Sprite Icon { get; }

        public IReadOnlyList<string> EffectSummaries { get; }
    }

    public sealed class RelicPresentationLibrary
    {
        private readonly IReadOnlyDictionary<RelicId, RelicPresentation> _byId;

        public RelicPresentationLibrary(IReadOnlyDictionary<RelicId, RelicPresentation> byId)
        {
            _byId = byId;
        }

        public bool TryGet(RelicId id, out RelicPresentation presentation) => _byId.TryGetValue(id, out presentation);
    }
}
