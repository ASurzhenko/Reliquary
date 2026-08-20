namespace Reliquary.Domain
{
    /// <summary>
    /// A passive a relic (or, from P4, a completed set) grants. Effects only ever contribute to the shared
    /// modifier accumulator, so their order of application does not change the result.
    /// </summary>
    public interface IRelicEffect
    {
        void Apply(RelicModifiers modifiers);
    }
}
