using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Content
{
    [CreateAssetMenu(menuName = "Reliquary/Effects/Essence Yield", fileName = "Effect_EssenceYield")]
    public sealed class EssenceYieldEffectDefinition : RelicEffectDefinition
    {
        [SerializeField, Range(-0.5f, 1f)] private float _bonusFraction = 0.15f;

        public override string Summary => _bonusFraction < 0f
            ? $"Duplicates dissolve into {-_bonusFraction:P0} less essence."
            : $"Duplicates dissolve into {_bonusFraction:P0} more essence.";

        public override IRelicEffect CreateEffect()
        {
            return new EssenceYieldEffect(_bonusFraction);
        }

        private sealed class EssenceYieldEffect : IRelicEffect
        {
            private readonly float _bonusFraction;

            public EssenceYieldEffect(float bonusFraction)
            {
                _bonusFraction = bonusFraction;
            }

            public void Apply(RelicModifiers modifiers)
            {
                modifiers.AddEssenceBonus(_bonusFraction);
            }
        }
    }
}
