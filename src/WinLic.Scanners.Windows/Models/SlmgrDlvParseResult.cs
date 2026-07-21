using System;

namespace WinLic.Scanners.Windows.Models
{
    public sealed class SlmgrDlvParseResult
    {
        public bool Parsed { get; set; }
        public string Description { get; set; } = string.Empty;
        public string LicenseStatusText { get; set; } = string.Empty;
        public string PartialProductKey { get; set; } = string.Empty;
        public string KmsMachineName { get; set; } = string.Empty;
        public DateTimeOffset? ExpirationDate { get; set; }
    }
}
