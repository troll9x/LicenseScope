using System;
using System.Collections.Generic;

namespace LicenseScope.Core.Models
{
    public enum CrackTraceStatus
    {
        Clean,
        Suspicious,
        Detected,
        Unknown,
        Error
    }

    public enum CrackTraceVerdict
    {
        Clean,
        Suspicious,
        HighRisk,
        Inconclusive,
        ScanError
    }

    public sealed class CrackTraceCheckResult
    {
        public int Order { get; set; }
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public CrackTraceStatus Status { get; set; }
        public string Summary { get; set; } = string.Empty;
        public IReadOnlyList<string> Evidence { get; set; } = Array.Empty<string>();
        public int Confidence { get; set; }
        public bool IsStrongSignal { get; set; }
        public bool IsDefinitiveActiveSignal { get; set; }
        public bool IsDataIncomplete { get; set; }
    }

    public sealed class CrackTraceAnalysisResult
    {
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset CompletedAt { get; set; }
        public IReadOnlyList<CrackTraceCheckResult> Checks { get; set; } =
            Array.Empty<CrackTraceCheckResult>();
        public CrackTraceVerdict Verdict { get; set; }
        public string VerdictSummary { get; set; } = string.Empty;
    }

    public static class CrackTraceVerdictNames
    {
        public static string ToMachineValue(CrackTraceVerdict verdict)
        {
            switch (verdict)
            {
                case CrackTraceVerdict.Clean: return "CLEAN";
                case CrackTraceVerdict.Suspicious: return "SUSPICIOUS";
                case CrackTraceVerdict.HighRisk: return "HIGH_RISK";
                case CrackTraceVerdict.Inconclusive: return "INCONCLUSIVE";
                default: return "SCAN_ERROR";
            }
        }
    }
}
