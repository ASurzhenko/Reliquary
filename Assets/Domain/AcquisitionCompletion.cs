namespace Reliquary.Domain
{
    /// <summary>
    /// How a request ended. Five terminals, and every request raises exactly one of them: a shape with no
    /// terminal is a spinner that never stops and a button that never comes back.
    /// </summary>
    public enum AcquisitionCompletionReason
    {
        Granted,
        Rejected,
        Cancelled,
        Failed,

        /// <summary>The result arrived after the session had moved on. Nothing was written.</summary>
        Superseded
    }

    public sealed class AcquisitionCompletion
    {
        private AcquisitionCompletion(AcquisitionCompletionReason reason, RelicId relicId, bool wasFirstCopy,
            AcquisitionRejection rejection, string detail)
        {
            Reason = reason;
            RelicId = relicId;
            WasFirstCopy = wasFirstCopy;
            Rejection = rejection;
            Detail = detail;
        }

        public AcquisitionCompletionReason Reason { get; }

        public RelicId RelicId { get; }

        /// <summary>True when a granted relic was one the player did not own before.</summary>
        public bool WasFirstCopy { get; }

        public AcquisitionRejection Rejection { get; }

        /// <summary>Developer diagnostics. Never shown to a player.</summary>
        public string Detail { get; }

        public static AcquisitionCompletion Granted(InventoryChange change)
        {
            return new AcquisitionCompletion(AcquisitionCompletionReason.Granted, change.Id, change.WasFirstCopy,
                AcquisitionRejection.None, null);
        }

        public static AcquisitionCompletion Rejected(AcquisitionRejection rejection)
        {
            return new AcquisitionCompletion(AcquisitionCompletionReason.Rejected, default, false, rejection, null);
        }

        public static AcquisitionCompletion Cancelled()
        {
            return new AcquisitionCompletion(AcquisitionCompletionReason.Cancelled, default, false,
                AcquisitionRejection.None, null);
        }

        public static AcquisitionCompletion Failed(string detail)
        {
            return new AcquisitionCompletion(AcquisitionCompletionReason.Failed, default, false,
                AcquisitionRejection.None, detail);
        }

        public static AcquisitionCompletion Superseded(int requested, int current, RelicId relicId)
        {
            return new AcquisitionCompletion(AcquisitionCompletionReason.Superseded, relicId, false,
                AcquisitionRejection.None, $"generation {requested} of {current}");
        }

        /// <summary>Maps the shapes a service can answer with; a grant is completed by the coordinator.</summary>
        public static AcquisitionCompletion From(AcquisitionResult result)
        {
            switch (result.Outcome)
            {
                case AcquisitionOutcome.Rejected:
                    return Rejected(result.Rejection);

                case AcquisitionOutcome.Cancelled:
                    return Cancelled();

                case AcquisitionOutcome.Failed:
                    return Failed(result.Detail);

                default:
                    return Failed($"an acquisition result of '{result.Outcome}' has no completion of its own");
            }
        }
    }
}
