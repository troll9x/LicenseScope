using System;

namespace LicenseScope.Windows.Models
{
    public sealed class WindowsLicenseProductRecord
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string ActivationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public uint? LicenseStatus { get; set; }
        public string PartialProductKey { get; set; } = string.Empty;
        public string ProductKeyChannel { get; set; } = string.Empty;
        public uint? GracePeriodRemaining { get; set; }
        public DateTimeOffset? EvaluationEndDate { get; set; }
        public string KmsMachineName { get; set; } = string.Empty;
        public uint? KmsPort { get; set; }
    }
}
