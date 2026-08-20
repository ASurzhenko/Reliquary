using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Content
{
    [CreateAssetMenu(menuName = "Reliquary/Effects/Unowned Pull", fileName = "Effect_UnownedPull")]
    public sealed class UnownedPullEffectDefinition : RelicEffectDefinition
    {
        [SerializeField, Min(0)] private int _pullBonus = 100;

        public override string Summary => $"Relics you have not found are {_pullBonus} points likelier to appear.";

        public override IRelicEffect CreateEffect()
        {
            return new UnownedPullEffect(_pullBonus);
        }

        private sealed class UnownedPullEffect : IRelicEffect
        {
            private readonly int _pullBonus;

            public UnownedPullEffect(int pullBonus)
            {
                _pullBonus = pullBonus;
            }

            public void Apply(RelicModifiers modifiers)
            {
                modifiers.AddUnownedPullBonus(_pullBonus);
            }
        }
    }
}
