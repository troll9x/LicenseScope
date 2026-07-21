namespace WinLic.Core.Models
{
    /// <summary>Reports framework-neutral progress for an audit.</summary>
    public sealed class AuditProgress
    {
        public string ScannerId { get; set; } = string.Empty;
        public string ScannerName { get; set; } = string.Empty;
        public int CurrentIndex { get; set; }
        public int TotalScannerCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public double? Percentage { get; set; }
    }
}
