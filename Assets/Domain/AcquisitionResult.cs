namespace Reliquary.Domain
{
    public enum AcquisitionOutcome
    {
        Granted,
        Rejected,
        Cancelled,
        Failed
    }

    /// <summary>
    /// Why an acquisition was refused on a legitimate state. A reason, not a sentence: the layer that
    /// draws supplies the English, because player-facing copy does not belong to the rules.
    /// </summary>
    public enum AcquisitionRejection
    {
        None,

        /// <summary>Nothing in the catalogue can be drawn.</summary>
        CatalogueEmpty
    }

    /// <summary>What an acquisition service answered. Every shape a caller can receive is one of these.</summary>
    public sealed class AcquisitionResult
    {
        private AcquisitionResult(AcquisitionOutcome outcome, RelicId relicId, AcquisitionRejection rejection,
            string detail)
        {
            Outcome = outcome;
            RelicId = relicId;
            Rejection = rejection;
            Detail = detail;
        }

        public AcquisitionOutcome Outcome { get; }

        public RelicId RelicId { get; }

        public AcquisitionRejection Rejection { get; }

        /// <summary>Developer diagnostics for a failure. Never shown to a player.</summary>
        public string Detail { get; }

        public static AcquisitionResult Granted(RelicId relicId)
        {
            return new AcquisitionResult(AcquisitionOutcome.Granted, relicId, AcquisitionRejection.None, null);
        }

        public static AcquisitionResult Rejected(AcquisitionRejection rejection)
        {
            return new AcquisitionResult(AcquisitionOutcome.Rejected, default, rejection, null);
        }

        public static AcquisitionResult Cancelled()
        {
            return new AcquisitionResult(AcquisitionOutcome.Cancelled, default, AcquisitionRejection.None, null);
        }

        public static AcquisitionResult Failed(string detail)
        {
            return new AcquisitionResult(AcquisitionOutcome.Failed, default, AcquisitionRejection.None, detail);
        }
    }
}
