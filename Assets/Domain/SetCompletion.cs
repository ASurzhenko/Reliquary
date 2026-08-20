using System;

namespace Reliquary.Domain
{
    /// <summary>
    /// A set that just became complete. It carries the id and nothing else: a display name and a perk line
    /// are presentation, and the domain has no expression that produces either.
    /// </summary>
    public readonly struct SetCompletion
    {
        public SetCompletion(SetId id)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("A completion names the set that completed.", nameof(id));
            }

            Id = id;
        }

        public SetId Id { get; }
    }
}
