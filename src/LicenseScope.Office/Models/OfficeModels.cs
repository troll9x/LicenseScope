using System;
using System.Collections.Generic;

namespace LicenseScope.Office.Models
{
    public enum OfficeProductFamily { OfficeSuite, Microsoft365Apps, Project, Visio, Access, Unknown }

    public sealed class OfficeInstallation
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public OfficeProductFamily Family { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public string InstallationType { get; set; } = string.Empty;
        public string RootPath { get; set; } = string.Empty;
        public bool UsesVNext { get; set; }
    }

    public sealed class OfficeToolLocation
    {
        public string ToolType { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }

    public sealed class OfficeProductEvidence
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public OfficeProductFamily Family { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public string InstallationType { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string LicenseState { get; set; } = string.Empty;
        public string PartialProductKey { get; set; } = string.Empty;
        public DateTimeOffset? ExpirationDate { get; set; }
        public string ExpirationKind { get; set; } = string.Empty;
        public string MaskedAccount { get; set; } = string.Empty;
        public string LicenseMode { get; set; } = string.Empty;
        public bool FromOfficialTool { get; set; }
        public bool PartiallyParsed { get; set; }
    }

    public sealed class OfficeEvidence
    {
        public IReadOnlyList<OfficeInstallation> Installations { get; set; } = Array.Empty<OfficeInstallation>();
        public IReadOnlyList<OfficeProductEvidence> Products { get; set; } = Array.Empty<OfficeProductEvidence>();
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    }

    public sealed class OfficeCommandResult
    {
        public bool Success { get; set; }
        public string SanitizedOutput { get; set; } = string.Empty;
        public string Warning { get; set; } = string.Empty;
    }
}
