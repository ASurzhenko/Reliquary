using System.Collections.Generic;
using Reliquary.Domain;

namespace Reliquary.Content
{
    /// <summary>Everything a view needs to draw a set, resolved by id.</summary>
    public readonly struct SetPresentation
    {
        public SetPresentation(string displayName, string description, IReadOnlyList<string> perkSummaries,
            ModifierDimension dimensions)
        {
            DisplayName = displayName;
            Description = description;
            PerkSummaries = perkSummaries;
            Dimensions = dimensions;
        }

        public string DisplayName { get; }

        public string Description { get; }

        /// <summary>One line per perk, built from the perk asset's own numbers.</summary>
        public IReadOnlyList<string> PerkSummaries { get; }

        /// <summary>Which outputs this set's perks move, so a view can point at the right number.</summary>
        public ModifierDimension Dimensions { get; }
    }

    public sealed class SetPresentationLibrary
    {
        private readonly IReadOnlyDictionary<SetId, SetPresentation> _byId;

        public SetPresentationLibrary(IReadOnlyDictionary<SetId, SetPresentation> byId)
        {
            _byId = byId;
        }

        public bool TryGet(SetId id, out SetPresentation presentation) => _byId.TryGetValue(id, out presentation);
    }
}
