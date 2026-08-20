namespace Reliquary.Domain
{
    public enum RelicContentSeverity
    {
        Warning,
        Error
    }

    /// <summary>
    /// One problem found while building a catalogue. Diagnostic text for a developer console — not UI copy;
    /// nothing here is ever shown to a player. Callers decide how loud it gets.
    /// </summary>
    public sealed class RelicContentIssue
    {
        private RelicContentIssue(RelicContentSeverity severity, RelicId subject, string message)
        {
            Severity = severity;
            Subject = subject;
            Message = message;
        }

        public RelicContentSeverity Severity { get; }

        public RelicId Subject { get; }

        public string Message { get; }

        public static RelicContentIssue Error(RelicId subject, string message)
        {
            return new RelicContentIssue(RelicContentSeverity.Error, subject, message);
        }

        public static RelicContentIssue Warning(RelicId subject, string message)
        {
            return new RelicContentIssue(RelicContentSeverity.Warning, subject, message);
        }
    }
}
