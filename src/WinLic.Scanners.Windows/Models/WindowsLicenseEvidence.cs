using System;
using System.Collections.Generic;

namespace WinLic.Scanners.Windows.Models
{
    public sealed class WindowsLicenseEvidence
    {
        public IReadOnlyList<WindowsLicenseProductRecord> Products { get; set; } = Array.Empty<WindowsLicenseProductRecord>();
        public SlmgrXprParseResult Xpr { get; set; } = new SlmgrXprParseResult();
        public SlmgrDlvParseResult Dlv { get; set; } = new SlmgrDlvParseResult();
        public string MaskedOa3Key { get; set; } = string.Empty;
        public string MaskedBackupKey { get; set; } = string.Empty;
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    }
}
