using System;
using System.Collections.Generic;

namespace LicenseScope.Windows.Models
{
    public sealed class WindowsInspectionSettings
    {
        public IReadOnlyList<int> LocalPorts { get; set; } = Array.Empty<int>();
        public IReadOnlyList<string> ServiceKeywords { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> TaskKeywords { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> ProcessKeywords { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> FilePaths { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> KmsDomainKeywords { get; set; } = Array.Empty<string>();
    }

    public sealed class WindowsInspectionResult
    {
        public IReadOnlyList<LicenseScope.Core.Models.ScanEvidence> Evidence { get; set; } =
            Array.Empty<LicenseScope.Core.Models.ScanEvidence>();
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    }
}
