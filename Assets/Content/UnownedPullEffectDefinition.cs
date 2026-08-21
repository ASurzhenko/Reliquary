using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Content
{
    [CreateAssetMenu(menuName = "Reliquary/Effects/Unowned Pull", fileName = "Effect_UnownedPull")]
    public sealed class UnownedPullEffectDefinition : RelicEffectDefinition
    {
        [SerializeField, Min(0)] private int _pullBonus = 100;

        // The bonus is a weight added to the draw, and a weight means nothing to a player: the line says what
        // changes, not what the number is. Keeping it one line also keeps the perk row from wrapping.
        public override string Summary => "Relics you have not found surface far more often.";

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
