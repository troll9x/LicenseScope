using System;

namespace LicenseScope.Core.Models
{
    /// <summary>Records the outcome of one scanner invocation.</summary>
    public sealed class ScannerExecutionResult
    {
        public string ScannerId { get; set; } = string.Empty;
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset CompletedAt { get; set; }
        public bool WasApplicable { get; set; }
        public bool WasSuccessful { get; set; }
        public bool WasCancelled { get; set; }
        public string ErrorType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int ProductResultCount { get; set; }
    }
}
