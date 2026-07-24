using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Models;

namespace LicenseScope.Reporting
{
    internal static class ReportFile
    {
        public static async Task<ReportWriteResult> WriteAsync(
            string path,
            string content,
            bool overwrite,
            CancellationToken token,
            bool bom = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return Fail("Output path is required.");
                var full = Path.GetFullPath(path);
                var directory = Path.GetDirectoryName(full);
                if (string.IsNullOrWhiteSpace(directory)) return Fail("Invalid output path.");
                Directory.CreateDirectory(directory);
                if (File.Exists(full) && !overwrite) return Fail("Output file already exists.");
                var temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    token.ThrowIfCancellationRequested();
                    using (var writer = new StreamWriter(temporary, false, new UTF8Encoding(bom)))
                        await writer.WriteAsync(content).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    if (File.Exists(full)) File.Delete(full);
                    File.Move(temporary, full);
                    return new ReportWriteResult { Success = true, OutputPath = full };
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
            }
            catch (OperationCanceledException) { return Fail("Report writing was cancelled."); }
            catch (Exception ex) { return Fail("Report could not be written (" + ex.GetType().Name + ")."); }
        }

        private static ReportWriteResult Fail(string message) =>
            new ReportWriteResult { ErrorMessage = message };
    }

    internal static class Escape
    {
        public static string Json(string value) =>
            "\"" + (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t") + "\"";

        public static string Html(string value) =>
            (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");

        public static string Csv(string value)
        {
            var escaped = value ?? string.Empty;
            if (escaped.Length > 0 &&
                (escaped[0] == '=' || escaped[0] == '+' || escaped[0] == '-' ||
                 escaped[0] == '@' || escaped[0] == '\t' || escaped[0] == '\r'))
                escaped = "'" + escaped;
            return "\"" + escaped.Replace("\"", "\"\"") + "\"";
        }
    }

    public abstract class AuditReportWriterBase : IAuditReportWriter
    {
        private readonly IAuditResultSanitizer _sanitizer;

        protected AuditReportWriterBase(IAuditResultSanitizer sanitizer)
        {
            _sanitizer = sanitizer;
        }

        public abstract string FormatId { get; }
        protected abstract string Render(SanitizedAuditReport report, ReportWriteOptions options);

        public Task<ReportWriteResult> WriteAsync(
            AuditResult result,
            ReportWriteOptions options,
            CancellationToken token)
        {
            if (options == null)
                return Task.FromResult(new ReportWriteResult { ErrorMessage = "Options are required." });
            var snapshot = _sanitizer.CreateReportSnapshot(result, options);
            return ReportFile.WriteAsync(
                options.OutputPath,
                Render(snapshot, options),
                options.Overwrite,
                token,
                FormatId == "csv");
        }
    }

    public sealed class JsonAuditReportWriter : AuditReportWriterBase
    {
        public JsonAuditReportWriter(IAuditResultSanitizer sanitizer) : base(sanitizer) { }
        public override string FormatId => "json";

        protected override string Render(SanitizedAuditReport report, ReportWriteOptions options)
        {
            var summary = AuditSummary.From(report.Products);
            var products = string.Join(",", report.Products.Select(product =>
                "{\"scannerId\":" + Escape.Json(product.ScannerId) +
                ",\"vendor\":" + Escape.Json(product.Vendor) +
                ",\"productName\":" + Escape.Json(product.ProductName) +
                ",\"productVersion\":" + Escape.Json(product.ProductVersion) +
                ",\"installed\":" + product.Installed.ToString().ToLowerInvariant() +
                ",\"status\":" + Escape.Json(product.Status.ToString()) +
                ",\"isLicensed\":" +
                (product.IsLicensed.HasValue
                    ? product.IsLicensed.Value.ToString().ToLowerInvariant()
                    : "null") +
                ",\"licenseType\":" + Escape.Json(product.LicenseType) +
                ",\"partialProductKey\":" + Escape.Json(product.PartialProductKey) +
                ",\"expirationDate\":" +
                (product.ExpirationDate.HasValue
                    ? Escape.Json(product.ExpirationDate.Value.ToString("o"))
                    : "null") +
                ",\"confidence\":" + Escape.Json(product.Confidence.ToString()) +
                ",\"warnings\":[" + string.Join(",", product.Warnings.Select(Escape.Json)) + "]" +
                ",\"evidence\":[" + string.Join(",", product.Evidence.Select(item =>
                    "{\"source\":" + Escape.Json(item.Source) +
                    ",\"name\":" + Escape.Json(item.Name) +
                    ",\"value\":" + Escape.Json(item.Value) + "}")) + "]}"));
            var executions = string.Join(",", report.ScannerExecutions.Select(execution =>
                "{\"scannerId\":" + Escape.Json(execution.ScannerId) +
                ",\"successful\":" + execution.WasSuccessful.ToString().ToLowerInvariant() +
                ",\"cancelled\":" + execution.WasCancelled.ToString().ToLowerInvariant() +
                ",\"productCount\":" + execution.ProductResultCount +
                ",\"error\":" + Escape.Json(execution.ErrorMessage) + "}"));
            return "{\"schemaVersion\":\"1.0\"" +
                   ",\"application\":{\"name\":\"License Scope\",\"version\":" +
                   Escape.Json(report.ApplicationVersion) + "}" +
                   ",\"audit\":{\"startedAt\":" + Escape.Json(report.StartedAt.ToString("o")) +
                   ",\"completedAt\":" + Escape.Json(report.CompletedAt.ToString("o")) +
                   ",\"durationMilliseconds\":" +
                   (long)(report.CompletedAt - report.StartedAt).TotalMilliseconds +
                   ",\"wasCancelled\":" + report.WasCancelled.ToString().ToLowerInvariant() + "}" +
                   ",\"system\":{\"operatingSystem\":" + Escape.Json(report.System.OsName) +
                   ",\"version\":" + Escape.Json(report.System.OsVersion) +
                   ",\"build\":" + Escape.Json(report.System.OsBuild) +
                   ",\"architecture\":" + Escape.Json(report.System.OsArchitecture.ToString()) +
                   ",\"machine\":" +
                   (string.IsNullOrEmpty(report.System.MachineName)
                       ? "null"
                       : Escape.Json(report.System.MachineName)) + "}" +
                   ",\"summary\":{\"total\":" + summary.Total +
                   ",\"licensed\":" + summary.Licensed +
                   ",\"unlicensed\":" + summary.Unlicensed +
                   ",\"expired\":" + summary.Expired +
                   ",\"attention\":" + summary.Attention +
                   ",\"unknown\":" + summary.Unknown + "}" +
                   ",\"products\":[" + products + "]" +
                   ",\"scannerExecutions\":[" + executions + "]" +
                   ",\"crackTrace\":" + RenderCrackTraceJson(report.CrackTraceAnalysis) + "}";
        }

        private static string RenderCrackTraceJson(CrackTraceAnalysisResult? analysis)
        {
            if (analysis == null) return "null";
            var checks = string.Join(",", analysis.Checks.OrderBy(x => x.Order).Select(check =>
                "{\"order\":" + check.Order +
                ",\"id\":" + Escape.Json(check.Id) +
                ",\"displayName\":" + Escape.Json(check.DisplayName) +
                ",\"status\":" + Escape.Json(check.Status.ToString()) +
                ",\"summary\":" + Escape.Json(check.Summary) +
                ",\"evidence\":[" + string.Join(",", check.Evidence.Select(Escape.Json)) + "]" +
                ",\"confidence\":" + check.Confidence +
                ",\"strongSignal\":" + check.IsStrongSignal.ToString().ToLowerInvariant() +
                ",\"definitiveActiveSignal\":" +
                check.IsDefinitiveActiveSignal.ToString().ToLowerInvariant() +
                ",\"dataIncomplete\":" + check.IsDataIncomplete.ToString().ToLowerInvariant() + "}"));
            var coverage = string.Join(",", analysis.DetectionCoverage.Select(item =>
                "{\"id\":" + Escape.Json(item.Id) +
                ",\"name\":" + Escape.Json(item.DisplayName) +
                ",\"status\":" +
                Escape.Json(CrackTraceVerdictNames.ToMachineValue(item.Status)) +
                ",\"detail\":" + Escape.Json(item.Detail) + "}"));
            var traceVerdict =
                CrackTraceVerdictNames.ToMachineValue(analysis.TraceVerdict);
            return "{\"activationState\":" +
                   Escape.Json(CrackTraceVerdictNames.ToMachineValue(
                       analysis.ActivationState)) +
                   ",\"traceVerdict\":" + Escape.Json(traceVerdict) +
                   ",\"verdict\":" + Escape.Json(traceVerdict) +
                   ",\"provenanceVerdict\":" +
                   Escape.Json(CrackTraceVerdictNames.ToMachineValue(
                       analysis.ProvenanceVerdict)) +
                   ",\"detectionCoverage\":[" + coverage + "]" +
                   ",\"blindSpots\":[" +
                   string.Join(",", analysis.BlindSpots.Select(Escape.Json)) + "]" +
                   ",\"evidence\":[" +
                   string.Join(",", analysis.Evidence.Select(Escape.Json)) + "]" +
                   ",\"confidence\":" + analysis.Confidence +
                   ",\"deepForensicScanEnabled\":" +
                   analysis.DeepForensicScanEnabled.ToString().ToLowerInvariant() +
                   ",\"summary\":" + Escape.Json(analysis.VerdictSummary) +
                   ",\"checks\":[" + checks + "]}";
        }
    }

    public sealed class CsvAuditReportWriter : AuditReportWriterBase
    {
        public CsvAuditReportWriter(IAuditResultSanitizer sanitizer) : base(sanitizer) { }
        public override string FormatId => "csv";

        protected override string Render(SanitizedAuditReport report, ReportWriteOptions options)
        {
            var builder = new StringBuilder(
                "ScanTime,ScannerId,Vendor,ProductName,ProductVersion,Installed,Status,IsLicensed,LicenseType,ExpirationDate,Confidence,Warnings\r\n");
            foreach (var product in report.Products)
                builder.AppendLine(string.Join(",", new[]
                {
                    Escape.Csv(report.CompletedAt.ToString("o")),
                    Escape.Csv(product.ScannerId),
                    Escape.Csv(product.Vendor),
                    Escape.Csv(product.ProductName),
                    Escape.Csv(product.ProductVersion),
                    Escape.Csv(product.Installed.ToString()),
                    Escape.Csv(product.Status.ToString()),
                    Escape.Csv(product.IsLicensed?.ToString() ?? string.Empty),
                    Escape.Csv(product.LicenseType),
                    Escape.Csv(product.ExpirationDate?.ToString("o") ?? string.Empty),
                    Escape.Csv(product.Confidence.ToString()),
                    Escape.Csv(string.Join(" | ", product.Warnings))
                }));
            if (report.CrackTraceAnalysis != null)
            {
                builder.AppendLine();
                builder.AppendLine(
                    "ActivationState,TraceVerdict,ProvenanceVerdict,DetectionCoverage,BlindSpots,Evidence,Confidence,CrackTraceSummary");
                builder.AppendLine(string.Join(",", new[]
                {
                    Escape.Csv(CrackTraceVerdictNames.ToMachineValue(
                        report.CrackTraceAnalysis.ActivationState)),
                    Escape.Csv(CrackTraceVerdictNames.ToMachineValue(
                        report.CrackTraceAnalysis.TraceVerdict)),
                    Escape.Csv(CrackTraceVerdictNames.ToMachineValue(
                        report.CrackTraceAnalysis.ProvenanceVerdict)),
                    Escape.Csv(string.Join(" | ",
                        report.CrackTraceAnalysis.DetectionCoverage.Select(x =>
                            x.DisplayName + ": " +
                            CrackTraceVerdictNames.ToMachineValue(x.Status)))),
                    Escape.Csv(string.Join(" | ",
                        report.CrackTraceAnalysis.BlindSpots)),
                    Escape.Csv(string.Join(" | ",
                        report.CrackTraceAnalysis.Evidence)),
                    Escape.Csv(report.CrackTraceAnalysis.Confidence.ToString()),
                    Escape.Csv(report.CrackTraceAnalysis.VerdictSummary)
                }));
                builder.AppendLine(
                    "CrackTraceOrder,CrackTraceId,CrackTraceName,CrackTraceStatus,CrackTraceConfidence,CrackTraceEvidence");
                foreach (var check in report.CrackTraceAnalysis.Checks.OrderBy(x => x.Order))
                    builder.AppendLine(string.Join(",", new[]
                    {
                        Escape.Csv(check.Order.ToString()),
                        Escape.Csv(check.Id),
                        Escape.Csv(check.DisplayName),
                        Escape.Csv(check.Status.ToString()),
                        Escape.Csv(check.Confidence.ToString()),
                        Escape.Csv(string.Join(" | ", check.Evidence))
                    }));
            }
            return builder.ToString();
        }
    }

    public sealed class HtmlAuditReportWriter : AuditReportWriterBase
    {
        public HtmlAuditReportWriter(IAuditResultSanitizer sanitizer) : base(sanitizer) { }
        public override string FormatId => "html";

        protected override string Render(SanitizedAuditReport report, ReportWriteOptions options)
        {
            var summary = AuditSummary.From(report.Products);
            var rows = string.Join("", report.Products.Select(product =>
                "<tr><td>" + Escape.Html(product.Vendor) + "</td><td>" +
                Escape.Html(product.ProductName) + "</td><td>" +
                Escape.Html(product.ProductVersion) + "</td><td>" +
                Escape.Html(product.Status.ToString()) + "</td><td>" +
                Escape.Html(product.LicenseType) + "</td><td>" +
                Escape.Html(product.Confidence.ToString()) + "</td><td>" +
                Escape.Html(string.Join("; ", product.Warnings)) + "</td></tr>"));
            var executions = string.Join("", report.ScannerExecutions.Select(execution =>
                "<li>" + Escape.Html(execution.ScannerId) + ": " +
                (execution.WasSuccessful ? "Success" : "Failed") + " " +
                Escape.Html(execution.ErrorMessage) + "</li>"));
            var crackTrace = RenderCrackTraceHtml(report.CrackTraceAnalysis);
            return "<!doctype html><html><head><meta charset=\"utf-8\">" +
                   "<title>License Scope</title><style>" +
                   "body{font-family:Segoe UI,sans-serif;margin:24px;color:#242038}" +
                   ".cards{display:flex;gap:12px}.card{padding:12px;border:1px solid #ddd;border-radius:8px}" +
                   "table{border-collapse:collapse;width:100%;margin-top:18px}" +
                   "th,td{border:1px solid #777;padding:8px;text-align:left}th{background:#f3f0fe}" +
                   ".trace{font-family:Consolas,monospace;border-left:5px solid #777;padding:8px;margin:8px 0}" +
                   ".trace.trace-not-found,.trace.tracenotfound{color:#1d4ed8;border-left-color:#3b82f6}" +
                   ".trace.suspicious{color:#854d0e;border-left-color:#ca8a04}" +
                   ".trace.detected,.trace.error{color:#991b1b;border-left-color:#dc2626}" +
                   ".trace.unknown{color:#4b5563;border-left-color:#6b7280}" +
                   ".evidence{color:#333}.verdict{font-weight:700;border:2px solid currentColor;padding:10px}" +
                   "@media print{body{margin:0}.trace{color:#000!important;border-left-color:#000!important}}" +
                   "</style></head><body><h1>License Scope</h1><p>Completed: " +
                   Escape.Html(report.CompletedAt.ToString("o")) +
                   (report.WasCancelled ? " — Cancelled" : string.Empty) +
                   "</p><div class=\"cards\"><div class=\"card\">Total: " + summary.Total +
                   "</div><div class=\"card\">Licensed: " + summary.Licensed +
                   "</div><div class=\"card\">Attention: " + summary.Attention +
                   "</div></div><table><thead><tr><th>Vendor</th><th>Product</th>" +
                   "<th>Version</th><th>Status</th><th>License type</th><th>Confidence</th>" +
                   "<th>Warnings</th></tr></thead><tbody>" + rows +
                   "</tbody></table>" + crackTrace +
                   "<h2>Scanner executions</h2><ul>" + executions + "</ul></body></html>";
        }

        private static string RenderCrackTraceHtml(CrackTraceAnalysisResult? analysis)
        {
            if (analysis == null) return string.Empty;
            var checks = string.Join("", analysis.Checks.OrderBy(x => x.Order).Select(check =>
                "<div class=\"trace " + check.Status.ToString().ToLowerInvariant() +
                "\" data-check-id=\"" + Escape.Html(check.Id) + "\"><strong>" +
                Escape.Html(CrackTraceTextFormatter.Prefix(check.Status) + " " +
                            check.Order + ". " + check.DisplayName + ": " + check.Summary) +
                "</strong><div class=\"evidence\">" +
                string.Join("", check.Evidence.Select(item =>
                    "<div>- " + Escape.Html(item) + "</div>")) + "</div></div>"));
            var verdictClass = analysis.TraceVerdict == CrackTraceVerdict.TraceNotFound
                ? "trace-not-found"
                : analysis.TraceVerdict == CrackTraceVerdict.Suspicious
                    ? "suspicious"
                    : analysis.TraceVerdict == CrackTraceVerdict.HighRisk
                        ? "detected"
                        : analysis.TraceVerdict == CrackTraceVerdict.Inconclusive
                            ? "unknown"
                            : "error";
            var coverage = "<h3>Coverage</h3><ul>" +
                           string.Join("", analysis.DetectionCoverage.Select(item =>
                               "<li>" + Escape.Html(item.DisplayName) + ": " +
                               Escape.Html(CoverageText(item.Status)) + "</li>")) +
                           "</ul>";
            var blindSpots = "<h3>Blind spots</h3><ul>" +
                             string.Join("", analysis.BlindSpots.Select(item =>
                                 "<li>" + Escape.Html(item) + "</li>")) +
                             "</ul>";
            var evidence = "<h3>Evidence</h3><ul>" +
                           string.Join("", analysis.Evidence.Select(item =>
                               "<li>" + Escape.Html(item) + "</li>")) +
                           "</ul>";
            return "<section><h2>Phân tích dấu vết crack Windows</h2>" + checks +
                   "<dl><dt>ActivationState</dt><dd>" +
                   Escape.Html(CrackTraceVerdictNames.ToMachineValue(
                       analysis.ActivationState)) +
                   "</dd><dt>TraceVerdict</dt><dd>" +
                   Escape.Html(CrackTraceVerdictNames.ToMachineValue(
                       analysis.TraceVerdict)) +
                   "</dd><dt>ProvenanceVerdict</dt><dd>" +
                   Escape.Html(CrackTraceVerdictNames.ToMachineValue(
                       analysis.ProvenanceVerdict)) +
                   "</dd><dt>Confidence</dt><dd>" + analysis.Confidence +
                   "</dd></dl>" + coverage + blindSpots + evidence +
                   "<p class=\"verdict trace " + verdictClass + "\">" +
                   Escape.Html("=> " + CrackTraceVerdictNames.ToMachineValue(
                                   analysis.TraceVerdict) +
                               ": " + analysis.VerdictSummary) + "</p></section>";
        }

        private static string CoverageText(DetectionCoverageStatus status)
        {
            switch (status)
            {
                case DetectionCoverageStatus.Checked: return "Checked";
                case DetectionCoverageStatus.NotChecked: return "Not checked";
                case DetectionCoverageStatus.NotTechnicallyVerifiable:
                    return "Not technically verifiable";
                default: return "Unknown";
            }
        }
    }
}
