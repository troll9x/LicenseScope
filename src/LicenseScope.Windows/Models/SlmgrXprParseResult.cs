using System;

namespace LicenseScope.Windows.Models
{
    public sealed class SlmgrXprParseResult
    {
        public bool Parsed { get; set; }
        public bool IsPermanent { get; set; }
        public bool IndicatesUnlicensed { get; set; }
        public DateTimeOffset? ExpirationDate { get; set; }
        public string RawSummary { get; set; } = string.Empty;
    }
}
