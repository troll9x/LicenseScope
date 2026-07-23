using System;
using System.Collections.Generic;

namespace LicenseScope.Windows.Models
{
    public sealed class WindowsActivationTrace
    {
        public string ProductName { get; set; } = string.Empty;
        public string EditionDescription { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string ActivationId { get; set; } = string.Empty;
        public string PartialProductKey { get; set; } = string.Empty;
        public uint? LicenseStatus { get; set; }
        public uint? GracePeriodRemaining { get; set; }
        public DateTimeOffset? ExpirationDate { get; set; }
        public bool IsPermanent { get; set; }
        public bool IndicatesUnlicensed { get; set; }
        public bool OemFirmwareKeyPresent { get; set; }
        public string KmsHost { get; set; } = string.Empty;
        public uint? KmsPort { get; set; }
    }

    public sealed class CrackTraceArtifact
    {
        public string Source { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string MatchedKeyword { get; set; } = string.Empty;
        public bool NameMatched { get; set; }
        public bool ActionMatched { get; set; }
    }

    public sealed class CrackTraceRegistryEvidence
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool Present { get; set; }
    }

    public sealed class CrackTraceEvidenceSnapshot
    {
        public WindowsActivationTrace Activation { get; set; } = new WindowsActivationTrace();
        public IReadOnlyList<CrackTraceArtifact> Services { get; set; } = Array.Empty<CrackTraceArtifact>();
        public IReadOnlyList<CrackTraceArtifact> Processes { get; set; } = Array.Empty<CrackTraceArtifact>();
        public IReadOnlyList<CrackTraceArtifact> Tasks { get; set; } = Array.Empty<CrackTraceArtifact>();
        public IReadOnlyList<CrackTraceArtifact> Paths { get; set; } = Array.Empty<CrackTraceArtifact>();
        public IReadOnlyList<CrackTraceArtifact> Events { get; set; } = Array.Empty<CrackTraceArtifact>();
        public IReadOnlyList<CrackTraceRegistryEvidence> RegistryValues { get; set; } =
            Array.Empty<CrackTraceRegistryEvidence>();
        public IReadOnlyList<string> UnavailableSources { get; set; } = Array.Empty<string>();
    }
}
