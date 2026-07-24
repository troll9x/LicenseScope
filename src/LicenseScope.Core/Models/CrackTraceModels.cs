using System;
using System.Collections.Generic;

namespace LicenseScope.Core.Models
{
    public enum CrackTraceStatus
    {
        TraceNotFound,
        Suspicious,
        Detected,
        Unknown,
        Error
    }

    public enum CrackTraceVerdict
    {
        TraceNotFound,
        TraceDetected
    }

    public enum WindowsActivationState
    {
        Unknown,
        Activated,
        NotActivated
    }

    public enum LicenseProvenanceVerdict
    {
        Unverified,
        ConsistentState,
        Inconclusive
    }

    public enum DetectionCoverageStatus
    {
        Checked,
        NotChecked,
        NotTechnicallyVerifiable,
        Unknown
    }

    public sealed class CrackTraceScanOptions
    {
        public bool DeepForensicScan { get; set; }
        public bool UserConsented { get; set; }
    }

    public sealed class DetectionCoverageItem
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DetectionCoverageStatus Status { get; set; }
        public bool Checked { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

    public sealed class CrackTraceCheckResult
    {
        public int Order { get; set; }
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public CrackTraceStatus Status { get; set; }
        public string Summary { get; set; } = string.Empty;
        public IReadOnlyList<string> Evidence { get; set; } = Array.Empty<string>();
        public bool Completed { get; set; }
        public bool Matched { get; set; }
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
        public bool ScanCompleted { get; set; }
        public bool ActivationDetected { get; set; }
        public bool TraceDetected { get; set; }
        public bool ProvenanceVerified { get; set; }
        public WindowsActivationState ActivationState { get; set; }
        public CrackTraceVerdict TraceVerdict { get; set; }
        public LicenseProvenanceVerdict ProvenanceVerdict { get; set; }
        public IReadOnlyList<DetectionCoverageItem> DetectionCoverage { get; set; } =
            Array.Empty<DetectionCoverageItem>();
        public IReadOnlyList<string> BlindSpots { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> Evidence { get; set; } = Array.Empty<string>();
        public int Confidence { get; set; }
        public bool DeepForensicScanEnabled { get; set; }
        public string VerdictSummary { get; set; } = string.Empty;
    }

    public static class CrackTraceVerdictNames
    {
        public static string ToMachineValue(CrackTraceVerdict verdict)
        {
            switch (verdict)
            {
                case CrackTraceVerdict.TraceDetected: return "TRACE_DETECTED";
                default: return "TRACE_NOT_DETECTED";
            }
        }

        public static string ToMachineValue(WindowsActivationState state)
        {
            switch (state)
            {
                case WindowsActivationState.Activated: return "ACTIVATED";
                case WindowsActivationState.NotActivated: return "NOT_ACTIVATED";
                default: return "UNKNOWN";
            }
        }

        public static string ToMachineValue(LicenseProvenanceVerdict verdict)
        {
            switch (verdict)
            {
                case LicenseProvenanceVerdict.ConsistentState: return "CONSISTENT_STATE";
                case LicenseProvenanceVerdict.Unverified: return "UNVERIFIED";
                default: return "INCONCLUSIVE";
            }
        }

        public static string ToMachineValue(DetectionCoverageStatus status)
        {
            switch (status)
            {
                case DetectionCoverageStatus.Checked: return "CHECKED";
                case DetectionCoverageStatus.NotChecked: return "NOT_CHECKED";
                case DetectionCoverageStatus.NotTechnicallyVerifiable:
                    return "NOT_TECHNICALLY_VERIFIABLE";
                default: return "UNKNOWN";
            }
        }
    }
}
