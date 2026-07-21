using System;
using System.Collections.Generic;

namespace WinLic.Core.Models
{
    /// <summary>Aggregate result of one complete audit attempt.</summary>
    public sealed class AuditResult
    {
        public SystemContext System { get; set; } = new SystemContext();
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset CompletedAt { get; set; }
        public bool WasCancelled { get; set; }
        public IReadOnlyList<LicenseResult> Products { get; set; } = Array.Empty<LicenseResult>();
        public IReadOnlyList<ScannerExecutionResult> ScannerExecutions { get; set; } = Array.Empty<ScannerExecutionResult>();
    }
}
