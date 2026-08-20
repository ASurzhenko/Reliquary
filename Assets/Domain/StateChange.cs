using System;

namespace Reliquary.Domain
{
    /// <summary>
    /// One atomic movement of state: at most one relic copy and at most one essence amount, applied together
    /// or not at all. Built by the rules that validated it; consumed only by StatePersistence.TryApply.
    /// </summary>
    public readonly struct StateChange
    {
        private StateChange(RelicId relic, int copyDelta, int essenceDelta, EssenceChangeReason reason)
        {
            Relic = relic;
            CopyDelta = copyDelta;
            EssenceDelta = essenceDelta;
            Reason = reason;
        }

        /// <summary>A spare copy leaves, essence arrives.</summary>
        public static StateChange Dissolve(RelicId relic, int essenceGained)
        {
            if (!relic.IsValid)
            {
                throw new ArgumentException("A dissolve needs the relic being consumed.", nameof(relic));
            }

            if (essenceGained <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(essenceGained), essenceGained,
                    "A dissolve that yields nothing is refused before it becomes a change.");
            }

            return new StateChange(relic, -1, essenceGained, EssenceChangeReason.Dissolved);
        }

        /// <summary>Essence leaves, a copy arrives.</summary>
        public static StateChange Purchase(RelicId relic, int essenceSpent)
        {
            if (!relic.IsValid)
            {
                throw new ArgumentException("A purchase needs the relic being bought.", nameof(relic));
            }

            if (essenceSpent < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(essenceSpent), essenceSpent,
                    "A price cannot be negative.");
            }

            return new StateChange(relic, 1, -essenceSpent, EssenceChangeReason.Spent);
        }

        /// <summary>
        /// Essence moves and no copy does. What the editor diagnostic constructs, so that the diagnostic
        /// takes the same write a dissolve takes rather than a shortcut around the mechanism under test.
        /// </summary>
        public static StateChange Grant(int essence)
        {
            return new StateChange(default, 0, essence, EssenceChangeReason.Granted);
        }

        /// <summary>The relic a copy moved for; the default id when no copy moved.</summary>
        public RelicId Relic { get; }

        public int CopyDelta { get; }

        public int EssenceDelta { get; }

        public EssenceChangeReason Reason { get; }
    }
}
