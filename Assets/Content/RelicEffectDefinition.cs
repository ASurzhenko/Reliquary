using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Content
{
    /// <summary>
    /// Base type for every authored behaviour. A new behaviour is one new file: subclass this, serialize its
    /// numbers, and return the effect the rules will run. Nothing else in the project changes.
    /// </summary>
    public abstract class RelicEffectDefinition : ScriptableObject
    {
        /// <summary>One line of UI copy, built from this asset's own numbers.</summary>
        public abstract string Summary { get; }

        public abstract IRelicEffect CreateEffect();
    }
}
