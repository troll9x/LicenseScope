using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LicenseScope.Core.Models;
using LicenseScope.Reporting;

namespace LicenseScope.Reporting.Tests
{
    [TestClass]
    public sealed class CrackTraceReportingTests
    {
        [TestMethod]
        public void TextFormatterUsesAllRequiredPrefixes()
        {
            var analysis = Analysis(CrackTraceVerdict.Suspicious,
                Enum.GetValues(typeof(CrackTraceStatus))
                    .Cast<CrackTraceStatus>()
                    .Select((status, index) => Check(index + 1, status))
                    .ToArray());
            var text = string.Join("\n",
                new CrackTraceTextFormatter().Format(analysis).Select(x => x.Text));
            StringAssert.Contains(text, "[+]");
            StringAssert.Contains(text, "[!]");
            StringAssert.Contains(text, "[-]");
            StringAssert.Contains(text, "[?]");
            StringAssert.Contains(text, "[x]");
        }

        [TestMethod]
        public void JsonCsvAndHtmlContainSameVerdictAndSevenChecks()
        {
            var checks = Enumerable.Range(1, 7)
                .Select(order => Check(order, CrackTraceStatus.TraceNotFound))
                .ToArray();
            var audit = new AuditResult
            {
                StartedAt = DateTimeOffset.Parse("2026-01-02T03:04:05Z"),
                CompletedAt = DateTimeOffset.Parse("2026-01-02T03:05:05Z"),
                CrackTraceAnalysis = Analysis(CrackTraceVerdict.HighRisk, checks)
            };
            var sanitizer = new AuditResultSanitizer();
            var json = Write(new JsonAuditReportWriter(sanitizer), "json", audit);
            var csv = Write(new CsvAuditReportWriter(sanitizer), "csv", audit);
            var html = Write(new HtmlAuditReportWriter(sanitizer), "html", audit);
            foreach (var output in new[] { json, csv, html })
            {
                StringAssert.Contains(output, "HIGH_RISK");
                StringAssert.Contains(output, "ACTIVATED");
                StringAssert.Contains(output, "UNVERIFIED");
                StringAssert.Contains(output, "Current licensing state");
                StringAssert.Contains(output, "Digital-license provenance");
                for (var order = 1; order <= 7; order++)
                    StringAssert.Contains(output, "check-" + order);
            }
            StringAssert.Contains(json, "\"status\":\"TraceNotFound\"");
            StringAssert.Contains(json, "\"traceVerdict\":\"HIGH_RISK\"");
            StringAssert.Contains(json, "\"detectionCoverage\"");
            StringAssert.Contains(json, "\"blindSpots\"");
            StringAssert.Contains(json, "\"evidence\"");
            StringAssert.Contains(json, "\"confidence\"");
            StringAssert.Contains(html, "class=\"trace tracenotfound\"");
        }

        [TestMethod]
        public void GuiCliJsonCsvAndHtmlUseTheSameTraceVerdict()
        {
            var analysis = Analysis(
                CrackTraceVerdict.Inconclusive,
                new[] { Check(1, CrackTraceStatus.TraceNotFound) });
            var audit = new AuditResult { CrackTraceAnalysis = analysis };
            var text = string.Join(
                "\n",
                new CrackTraceTextFormatter().Format(analysis).Select(x => x.Text));
            var json = Write(
                new JsonAuditReportWriter(new AuditResultSanitizer()),
                "json",
                audit);
            var csv = Write(
                new CsvAuditReportWriter(new AuditResultSanitizer()),
                "csv",
                audit);
            var html = Write(
                new HtmlAuditReportWriter(new AuditResultSanitizer()),
                "html",
                audit);
            foreach (var output in new[] { text, json, csv, html })
                StringAssert.Contains(output, "INCONCLUSIVE");
            Assert.IsFalse(text.Contains("AN TOÀN"));
        }

        [TestMethod]
        public void CrackTraceReportsNeverExposeFullProductKeyOrSecrets()
        {
            var check = Check(1, CrackTraceStatus.Suspicious);
            check.Evidence = new[]
            {
                "key=AAAAA-BBBBB-CCCCC-DDDDD-ABCDE",
                "token=super-secret",
                @"C:\Users\alice\private"
            };
            var analysis = Analysis(CrackTraceVerdict.Suspicious, new[] { check });
            analysis.Evidence = new[]
            {
                "account=private.user@example.com",
                "key=AAAAA-BBBBB-CCCCC-DDDDD-ABCDE"
            };
            var audit = new AuditResult
            {
                CrackTraceAnalysis = analysis
            };
            var json = Write(
                new JsonAuditReportWriter(new AuditResultSanitizer()),
                "json",
                audit);
            Assert.IsFalse(json.Contains("AAAAA-BBBBB"));
            Assert.IsFalse(json.Contains("super-secret"));
            Assert.IsFalse(json.Contains("private.user@example.com"));
            Assert.IsFalse(json.Contains(@"\alice\"));
            StringAssert.Contains(json, "XXXXX-XXXXX-XXXXX-XXXXX-ABCDE");
            StringAssert.Contains(json, "[REDACTED]");
        }

        private static CrackTraceCheckResult Check(int order, CrackTraceStatus status)
        {
            return new CrackTraceCheckResult
            {
                Order = order,
                Id = "check-" + order,
                DisplayName = "Check " + order,
                Status = status,
                Summary = "summary",
                Confidence = 50
            };
        }

        private static CrackTraceAnalysisResult Analysis(
            CrackTraceVerdict verdict,
            CrackTraceCheckResult[] checks)
        {
            return new CrackTraceAnalysisResult
            {
                ActivationState = WindowsActivationState.Activated,
                TraceVerdict = verdict,
                ProvenanceVerdict = LicenseProvenanceVerdict.Unverified,
                DetectionCoverage = new[]
                {
                    new DetectionCoverageItem
                    {
                        Id = "current-licensing-state",
                        DisplayName = "Current licensing state",
                        Status = DetectionCoverageStatus.Checked
                    },
                    new DetectionCoverageItem
                    {
                        Id = "digital-license-provenance",
                        DisplayName = "Digital-license provenance",
                        Status = DetectionCoverageStatus.NotTechnicallyVerifiable
                    }
                },
                BlindSpots = new[] { "provenance blind spot" },
                Evidence = new[] { "Activation state: ACTIVATED" },
                Confidence = 50,
                VerdictSummary = "verdict summary",
                Checks = checks
            };
        }

        private static string Write(
            IAuditReportWriter writer,
            string extension,
            AuditResult audit)
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "." + extension);
            try
            {
                var result = writer.WriteAsync(
                    audit,
                    new ReportWriteOptions
                    {
                        OutputPath = path,
                        IncludeEvidence = true,
                        Overwrite = true
                    },
                    CancellationToken.None).Result;
                Assert.IsTrue(result.Success);
                return File.ReadAllText(path, Encoding.UTF8);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
