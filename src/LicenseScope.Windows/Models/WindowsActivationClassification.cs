using System;
using System.Collections.Generic;
using LicenseScope.Core.Models;

namespace LicenseScope.Windows.Models
{
    public sealed class WindowsActivationClassification
    {
        public LicenseStatus Status { get; set; } = LicenseStatus.Unknown;
        public string LicenseType { get; set; } = "Unknown";
        public string ActivationMethod { get; set; } = "Unknown";
        public ConfidenceLevel Confidence { get; set; }
        public DateTimeOffset? ExpirationDate { get; set; }
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    }
}
