using System;

namespace LicenseScope.Core.Runtime
{
    /// <summary>Captured process outcome, including failures that occurred before process start.</summary>
    public sealed class ProcessExecutionResult
    {
        public int? ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public bool TimedOut { get; set; }
        public bool WasCancelled { get; set; }
        public bool StartFailure { get; set; }
        public string StartFailureMessage { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
    }
}
