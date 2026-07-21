using System;

namespace WinLic.Core.Runtime
{
    /// <summary>Safe process invocation settings. Arguments must already be validated by the caller.</summary>
    public sealed class ProcessExecutionRequest
    {
        public string ExecutablePath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool RedirectStandardOutput { get; set; } = true;
        public bool RedirectStandardError { get; set; } = true;
        public bool CreateNoWindow { get; set; } = true;
    }
}
