using System;
using System.Linq;
using System.Text.RegularExpressions;
using LicenseScope.Core.Models;
using LicenseScope.Core.Security;

namespace LicenseScope.Reporting
{
    public sealed class AuditResultSanitizer : IAuditResultSanitizer
    {
        private static readonly Regex Email = new Regex(@"(?i)\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b", RegexOptions.Compiled);
        private static readonly Regex Guid = new Regex(@"(?i)\b[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\b", RegexOptions.Compiled);
        public SanitizedAuditReport CreateReportSnapshot(AuditResult result, ReportWriteOptions options)
        {
            if (result == null) throw new ArgumentNullException(nameof(result)); options = options ?? new ReportWriteOptions(); var system=result.System ?? new SystemContext();
            return new SanitizedAuditReport { ApplicationVersion=typeof(AuditResultSanitizer).Assembly.GetName().Version?.ToString() ?? "1.0.0.0",StartedAt=result.StartedAt,CompletedAt=result.CompletedAt,WasCancelled=result.WasCancelled,System=new SystemContext { MachineName=options.IncludeMachineName?SensitiveDataMasker.AnonymizeMachineName(system.MachineName):string.Empty,OsName=Clean(system.OsName,"System"),OsVersion=Clean(system.OsVersion,"System"),OsBuild=system.OsBuild,OsArchitecture=system.OsArchitecture,ProcessArchitecture=system.ProcessArchitecture,Is64BitOperatingSystem=system.Is64BitOperatingSystem,Is64BitProcess=system.Is64BitProcess },Products=(result.Products??Array.Empty<LicenseResult>()).Select(p=>new LicenseResult { ScannerId=Clean(p.ScannerId,"ScannerId"),Vendor=Clean(p.Vendor,"Vendor"),ProductName=Clean(p.ProductName,"ProductName"),ProductVersion=Clean(p.ProductVersion,"Version"),Installed=p.Installed,Status=p.Status,IsLicensed=p.IsLicensed,LicenseType=Clean(p.LicenseType,"LicenseType"),Confidence=p.Confidence,PartialProductKey=SensitiveDataMasker.MaskProductKey(p.PartialProductKey),ExpirationDate=p.ExpirationDate,Evidence=options.IncludeEvidence?(p.Evidence??Array.Empty<ScanEvidence>()).Where(e=>!SensitiveDataMasker.IsSensitiveFieldName(e.Name)).Select(e=>new ScanEvidence { Source=Clean(e.Source,"Source"),Name=Clean(e.Name,"Name"),Value=Clean(e.Value,e.Name),Confidence=e.Confidence,Sensitive=e.Sensitive }).ToArray():Array.Empty<ScanEvidence>(),Warnings=options.IncludeWarnings?(p.Warnings??Array.Empty<string>()).Select(x=>Clean(x,"Warning")).ToArray():Array.Empty<string>(),ErrorCode=Clean(p.ErrorCode,"ErrorCode"),ErrorMessage=Clean(p.ErrorMessage,"Error")}).ToArray(),ScannerExecutions=(result.ScannerExecutions??Array.Empty<ScannerExecutionResult>()).Select(x=>new ScannerExecutionResult { ScannerId=Clean(x.ScannerId,"ScannerId"),StartedAt=x.StartedAt,CompletedAt=x.CompletedAt,WasApplicable=x.WasApplicable,WasSuccessful=x.WasSuccessful,WasCancelled=x.WasCancelled,ErrorType=Clean(x.ErrorType,"ErrorType"),ErrorMessage=Clean(x.ErrorMessage,"Error"),ProductResultCount=x.ProductResultCount }).ToArray(),CrackTraceAnalysis=SanitizeCrackTrace(result.CrackTraceAnalysis,options.IncludeEvidence) };
        }
        private static CrackTraceAnalysisResult? SanitizeCrackTrace(
            CrackTraceAnalysisResult? analysis,
            bool includeEvidence)
        {
            if (analysis == null) return null;
            return new CrackTraceAnalysisResult
            {
                StartedAt = analysis.StartedAt,
                CompletedAt = analysis.CompletedAt,
                ScanCompleted = analysis.ScanCompleted,
                ActivationDetected = analysis.ActivationDetected,
                TraceDetected = analysis.TraceDetected,
                ProvenanceVerified = analysis.ProvenanceVerified,
                ActivationState = analysis.ActivationState,
                TraceVerdict = analysis.TraceVerdict,
                ProvenanceVerdict = analysis.ProvenanceVerdict,
                DetectionCoverage = (analysis.DetectionCoverage ??
                    Array.Empty<DetectionCoverageItem>()).Select(x =>
                    new DetectionCoverageItem
                    {
                        Id = Clean(x.Id, "Id"),
                        DisplayName = Clean(x.DisplayName, "DisplayName"),
                        Status = x.Status,
                        Checked = x.Checked,
                        Detail = SensitiveDataMasker.SanitizeDiagnosticText(x.Detail)
                    }).ToArray(),
                BlindSpots = (analysis.BlindSpots ?? Array.Empty<string>())
                    .Select(SensitiveDataMasker.SanitizeDiagnosticText)
                    .ToArray(),
                Evidence = includeEvidence
                    ? (analysis.Evidence ?? Array.Empty<string>())
                        .Select(SensitiveDataMasker.SanitizeDiagnosticText)
                        .ToArray()
                    : Array.Empty<string>(),
                Confidence = analysis.Confidence,
                DeepForensicScanEnabled = analysis.DeepForensicScanEnabled,
                VerdictSummary =
                    SensitiveDataMasker.SanitizeDiagnosticText(analysis.VerdictSummary),
                Checks = (analysis.Checks ?? Array.Empty<CrackTraceCheckResult>())
                    .Select(x => new CrackTraceCheckResult
                    {
                        Order = x.Order,
                        Id = Clean(x.Id, "Id"),
                        DisplayName = Clean(x.DisplayName, "DisplayName"),
                        Status = x.Status,
                        Summary = SensitiveDataMasker.SanitizeDiagnosticText(x.Summary),
                        Completed = x.Completed,
                        Matched = x.Matched,
                        Evidence = includeEvidence
                            ? (x.Evidence ?? Array.Empty<string>())
                                .Select(SensitiveDataMasker.SanitizeDiagnosticText)
                                .ToArray()
                            : Array.Empty<string>(),
                        Confidence = x.Confidence,
                        IsStrongSignal = x.IsStrongSignal,
                        IsDefinitiveActiveSignal = x.IsDefinitiveActiveSignal,
                        IsDataIncomplete = x.IsDataIncomplete
                    }).ToArray()
            };
        }
        private static string Clean(string value,string name){if(SensitiveDataMasker.IsSensitiveFieldName(name))return "[REDACTED]";var v=SensitiveDataMasker.MaskWindowsPath(value??string.Empty);v=Email.Replace(v,m=>SensitiveDataMasker.MaskEmail(m.Value));return Guid.Replace(v,"********-****-****-****-************");}
    }
}
