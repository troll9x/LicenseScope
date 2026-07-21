using System;
using System.Collections.Generic;

namespace WinLic.Scanners.Autodesk.Models
{
    public sealed class AutodeskInstallation { public string ProductName { get; set; } = ""; public string Version { get; set; } = ""; public string ProductCode { get; set; } = ""; public string Source { get; set; } = ""; }
    public sealed class AutodeskToolInfo { public string Path { get; set; } = ""; public string Version { get; set; } = ""; public bool Exists { get; set; } }
    public sealed class AutodeskRegistration
    {
        public string FeatureId { get; set; } = ""; public string DefaultProductKey { get; set; } = ""; public string DefaultProductVersion { get; set; } = "";
        public string SelectedProductKey { get; set; } = ""; public string SelectedProductVersion { get; set; } = ""; public string DefaultProductCode { get; set; } = ""; public string SelectedProductCode { get; set; } = "";
        public int? LicenseMethodCode { get; set; } public IReadOnlyList<int> SupportedLicenseMethodCodes { get; set; } = Array.Empty<int>(); public int? LicenseServerTypeCode { get; set; } public bool HasConfiguredServers { get; set; }
    }
    public sealed class AutodeskEvidenceResult { public IReadOnlyList<AutodeskRegistration> Registrations { get; set; } = Array.Empty<AutodeskRegistration>(); public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>(); public bool Successful { get; set; } }
    public sealed class AutodeskServiceStatus { public bool Found { get; set; } public bool Running { get; set; } public string Version { get; set; } = ""; public string Warning { get; set; } = ""; }
}
