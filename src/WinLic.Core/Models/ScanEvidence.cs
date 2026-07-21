namespace WinLic.Core.Models
{
    /// <summary>Describes one sanitized fact used to derive a license result.</summary>
    public sealed class ScanEvidence
    {
        public string Source { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public ConfidenceLevel Confidence { get; set; }
        public bool Sensitive { get; set; }
    }
}
