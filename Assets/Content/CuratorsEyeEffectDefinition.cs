using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Content
{
    [CreateAssetMenu(menuName = "Reliquary/Effects/Curator's Eye", fileName = "Effect_CuratorsEye")]
    public sealed class CuratorsEyeEffectDefinition : RelicEffectDefinition
    {
        [SerializeField, Range(0f, 1f)] private float _essenceBonus = 0.1f;
        [SerializeField, Min(0)] private int _pullBonus = 75;

        public override string Summary =>
            $"Duplicates dissolve into {_essenceBonus:P0} more essence, and unfound relics surface more often.";

        public override IRelicEffect CreateEffect()
        {
            return new CuratorsEyeEffect(_essenceBonus, _pullBonus);
        }

        private sealed class CuratorsEyeEffect : IRelicEffect
        {
            private readonly float _essenceBonus;
            private readonly int _pullBonus;

            public CuratorsEyeEffect(float essenceBonus, int pullBonus)
            {
                _essenceBonus = essenceBonus;
                _pullBonus = pullBonus;
            }

            public void Apply(RelicModifiers modifiers)
            {
                modifiers.AddEssenceBonus(_essenceBonus);
                modifiers.AddUnownedPullBonus(_pullBonus);
            }
        }
    }
}
