using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Models;

namespace LicenseScope.Reporting
{
    public sealed class ReportWriteOptions
    {
        public string OutputPath { get; set; } = string.Empty;
        public bool IncludeEvidence { get; set; } = true;
        public bool IncludeWarnings { get; set; } = true;
        public bool IncludeMachineName { get; set; }
        public bool Overwrite { get; set; }
    }
    public sealed class ReportWriteResult { public bool Success { get; set; } public string OutputPath { get; set; } = string.Empty; public string ErrorMessage { get; set; } = string.Empty; }
    public interface IAuditReportWriter { string FormatId { get; } Task<ReportWriteResult> WriteAsync(AuditResult auditResult, ReportWriteOptions options, CancellationToken cancellationToken); }
    public interface IAuditResultSanitizer { SanitizedAuditReport CreateReportSnapshot(AuditResult result, ReportWriteOptions options); }
    public sealed class SanitizedAuditReport
    {
        public string SchemaVersion { get; set; } = "1.0"; public string ApplicationVersion { get; set; } = string.Empty;
        public DateTimeOffset StartedAt { get; set; } public DateTimeOffset CompletedAt { get; set; } public bool WasCancelled { get; set; }
        public SystemContext System { get; set; } = new SystemContext(); public IReadOnlyList<LicenseResult> Products { get; set; } = Array.Empty<LicenseResult>(); public IReadOnlyList<ScannerExecutionResult> ScannerExecutions { get; set; } = Array.Empty<ScannerExecutionResult>();
        public CrackTraceAnalysisResult? CrackTraceAnalysis { get; set; }
    }
    public sealed class AuditSummary
    {
        public int Total { get; set; } public int Licensed { get; set; } public int Unlicensed { get; set; } public int Expired { get; set; } public int Attention { get; set; } public int Unknown { get; set; }
        public static AuditSummary From(IEnumerable<LicenseResult> products)
        { var s=new AuditSummary(); foreach(var p in products ?? Array.Empty<LicenseResult>()){s.Total++;switch(p.Status){case LicenseStatus.Licensed:s.Licensed++;break;case LicenseStatus.Unlicensed:s.Unlicensed++;break;case LicenseStatus.Expired:s.Expired++;break;case LicenseStatus.Trial:case LicenseStatus.GracePeriod:case LicenseStatus.NeedsSignIn:case LicenseStatus.NeedsOnlineVerification:s.Attention++;break;case LicenseStatus.Unknown:case LicenseStatus.Error:case LicenseStatus.Unsupported:s.Unknown++;break;}}return s; }
    }
}
