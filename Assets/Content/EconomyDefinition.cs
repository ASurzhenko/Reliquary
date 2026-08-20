using UnityEngine;

namespace Reliquary.Content
{
    /// <summary>
    /// What the trader charges. A price is the relic's own essence value times this multiplier, floored at
    /// the price floor — so a targeted purchase costs roughly three duplicates of the same tier, and the
    /// cheapest relics are never nearly free.
    /// </summary>
    [CreateAssetMenu(menuName = "Reliquary/Economy", fileName = "Economy")]
    public sealed class EconomyDefinition : ScriptableObject
    {
        [SerializeField, Range(1f, 10f)] private float _priceMultiplier = 3f;
        [SerializeField, Min(1)] private int _priceFloor = 10;

        public float PriceMultiplier => _priceMultiplier;

        public int PriceFloor => _priceFloor;
    }
}
