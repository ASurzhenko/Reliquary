using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    /// <summary>
    /// An effect built without the engine — which is the point: the rules are testable on their own.
    /// </summary>
    internal sealed class FakeEffect : IRelicEffect
    {
        private readonly float _essenceBonus;
        private readonly int _unownedPullBonus;

        public FakeEffect(float essenceBonus = 0f, int unownedPullBonus = 0)
        {
            _essenceBonus = essenceBonus;
            _unownedPullBonus = unownedPullBonus;
        }

        public void Apply(RelicModifiers modifiers)
        {
            modifiers.AddEssenceBonus(_essenceBonus);
            modifiers.AddUnownedPullBonus(_unownedPullBonus);
        }
    }
}
