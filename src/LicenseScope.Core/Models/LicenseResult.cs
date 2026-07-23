using System;
using System.Collections.Generic;

namespace LicenseScope.Core.Models
{
    /// <summary>Structured result for one detected or expected product.</summary>
    public sealed class LicenseResult
    {
        public string ScannerId { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductVersion { get; set; } = string.Empty;
        public bool Installed { get; set; }
        public LicenseStatus Status { get; set; } = LicenseStatus.Unknown;
        public bool? IsLicensed { get; set; }
        public string LicenseType { get; set; } = string.Empty;
        public ConfidenceLevel Confidence { get; set; }
        public string PartialProductKey { get; set; } = string.Empty;
        /// <summary>
        /// Full product key when a documented local source exposes it. This value is kept
        /// in memory for an explicit UI reveal only and must never be copied to reports.
        /// </summary>
        public string FullProductKey { get; set; } = string.Empty;
        public DateTimeOffset? ExpirationDate { get; set; }
        public IReadOnlyList<ScanEvidence> Evidence { get; set; } = Array.Empty<ScanEvidence>();
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
