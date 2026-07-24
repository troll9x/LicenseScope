using System;
using System.Collections.Generic;
using LicenseScope.Core.Security;

namespace LicenseScope.Windows.Models
{
    public sealed class WindowsLicenseEvidence
    {
        public IReadOnlyList<WindowsLicenseProductRecord> Products { get; set; } = Array.Empty<WindowsLicenseProductRecord>();
        public SlmgrXprParseResult Xpr { get; set; } = new SlmgrXprParseResult();
        public SlmgrDlvParseResult Dlv { get; set; } = new SlmgrDlvParseResult();
        public string Oa3ProductKey { get; set; } = string.Empty;
        public string Oa3ProductKeyDescription { get; set; } = string.Empty;
        public string BackupProductKey { get; set; } = string.Empty;
        public string MaskedOa3Key => SensitiveDataMasker.MaskProductKey(Oa3ProductKey);
        public string MaskedBackupKey => SensitiveDataMasker.MaskProductKey(BackupProductKey);
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    }
}
