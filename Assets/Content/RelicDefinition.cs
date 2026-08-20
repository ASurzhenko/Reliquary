using System;
using System.Collections.Generic;
using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Content
{
    [CreateAssetMenu(menuName = "Reliquary/Relic", fileName = "Relic_")]
    public sealed class RelicDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField, Min(0)] private int _essenceValue = 10;
        [SerializeField, Min(1)] private int _discoveryWeight = 100;
        [SerializeField] private RelicEffectDefinition[] _effects;

        /// <summary>
        /// The id the catalogue will carry. RelicId does not trim, so this is the single place the trimming
        /// rule lives: the loader builds ids from it and the editor validator compares ids by it.
        /// </summary>
        public string TrimmedId => _id == null ? string.Empty : _id.Trim();

        public string DisplayName => _displayName;

        public string Description => _description;

        public Sprite Icon => _icon;

        public int EssenceValue => _essenceValue;

        public int DiscoveryWeight => _discoveryWeight;

        public IReadOnlyList<RelicEffectDefinition> Effects => _effects ?? Array.Empty<RelicEffectDefinition>();

        /// <summary>
        /// Converts this asset into the rules' view of a relic. Returns false with a reason when the asset is
        /// not authored well enough to be one — the caller reports it; nothing is guessed or defaulted.
        /// Stops at the first problem: the editor validator, not this method, is what reports every problem
        /// on every asset.
        /// </summary>
        public bool TryCreateRelic(out Relic relic, out string error)
        {
            relic = null;

            if (string.IsNullOrWhiteSpace(_id))
            {
                error = "Id is empty. Give the relic a stable id such as 'relic.sunken_crown'.";
                return false;
            }

            IReadOnlyList<RelicEffectDefinition> definitions = Effects;
            List<IRelicEffect> effects = new List<IRelicEffect>(definitions.Count);

            for (int i = 0; i < definitions.Count; i++)
            {
                RelicEffectDefinition definition = definitions[i];

                if (definition == null)
                {
                    error = $"Effect slot {i} is empty or points at an asset that no longer exists.";
                    return false;
                }

                effects.Add(definition.CreateEffect());
            }

            relic = new Relic(new RelicId(TrimmedId), _essenceValue, _discoveryWeight, effects);
            error = null;
            return true;
        }
    }
}
