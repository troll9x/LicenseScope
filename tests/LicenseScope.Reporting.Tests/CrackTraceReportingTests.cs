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
        public void TextFormatterUsesOnlyBinaryOutcomeFields()
        {
            var checks = Enum.GetValues(typeof(CrackTraceStatus))
                .Cast<CrackTraceStatus>()
                .Select((status, index) => Check(index + 1, status))
                .ToArray();
            var text = string.Join(
                "\n",
                new CrackTraceTextFormatter()
                    .Format(Analysis(CrackTraceVerdict.TraceDetected, checks))
                    .Select(x => x.Text));

            StringAssert.Contains(text, "Quét hoàn tất: CÓ");
            StringAssert.Contains(text, "Phát hiện kích hoạt: CÓ");
            StringAssert.Contains(text, "Phát hiện dấu vết: CÓ");
            StringAssert.Contains(text, "Xác minh nguồn gốc giấy phép: KHÔNG");
            Assert.IsFalse(text.Contains("ScanCompleted:"));
            Assert.IsFalse(text.Contains("TraceDetected:"));
            Assert.IsFalse(text.Contains("SUSPICIOUS"));
            Assert.IsFalse(text.Contains("HIGH_RISK"));
            Assert.IsFalse(text.Contains("INCONCLUSIVE"));
        }

        [TestMethod]
        public void JsonCsvAndHtmlContainSameBinaryFactsAndSevenChecks()
        {
            var checks = Enumerable.Range(1, 7)
                .Select(order => Check(
                    order,
                    order == 1
                        ? CrackTraceStatus.Detected
                        : CrackTraceStatus.TraceNotFound))
                .ToArray();
            var audit = new AuditResult
            {
                StartedAt = DateTimeOffset.Parse("2026-01-02T03:04:05Z"),
                CompletedAt = DateTimeOffset.Parse("2026-01-02T03:05:05Z"),
                CrackTraceAnalysis =
                    Analysis(CrackTraceVerdict.TraceDetected, checks)
            };
            var sanitizer = new AuditResultSanitizer();
            var json = Write(new JsonAuditReportWriter(sanitizer), "json", audit);
            var csv = Write(new CsvAuditReportWriter(sanitizer), "csv", audit);
            var html = Write(new HtmlAuditReportWriter(sanitizer), "html", audit);

            StringAssert.Contains(json, "\"scanCompleted\":true");
            StringAssert.Contains(json, "\"activationDetected\":true");
            StringAssert.Contains(json, "\"traceDetected\":true");
            StringAssert.Contains(json, "\"provenanceVerified\":false");
            StringAssert.Contains(json, "\"scanCompleted\":\"Quét hoàn tất\"");
            StringAssert.Contains(json, "\"traceDetected\":\"Phát hiện dấu vết\"");
            StringAssert.Contains(json, "\"matched\":true");
            StringAssert.Contains(json, "\"checked\":true");
            StringAssert.Contains(json, "\"uncheckedSourceDetails\"");
            Assert.IsFalse(json.Contains("\"traceVerdict\""));
            Assert.IsFalse(json.Contains("\"provenanceVerdict\""));

            StringAssert.Contains(csv, "Quét hoàn tất,Phát hiện kích hoạt,Phát hiện dấu vết,Xác minh nguồn gốc giấy phép");
            StringAssert.Contains(html, "<dt>Phát hiện dấu vết</dt><dd>CÓ");
            Assert.IsFalse(csv.Contains("ScanCompleted"));
            Assert.IsFalse(html.Contains("<dt>TraceDetected</dt>"));
            foreach (var output in new[] { json, csv, html })
            {
                for (var order = 1; order <= 7; order++)
                    StringAssert.Contains(output, "check-" + order);
                Assert.IsFalse(output.Contains("SUSPICIOUS"));
                Assert.IsFalse(output.Contains("HIGH_RISK"));
                Assert.IsFalse(output.Contains("INCONCLUSIVE"));
            }
        }

        [TestMethod]
        public void GuiCliJsonCsvAndHtmlUseTheSameBinaryTraceFact()
        {
            var analysis = Analysis(
                CrackTraceVerdict.TraceNotFound,
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

            StringAssert.Contains(text, "Phát hiện dấu vết: KHÔNG");
            StringAssert.Contains(json, "\"traceDetected\":false");
            StringAssert.Contains(csv, "Phát hiện dấu vết");
            StringAssert.Contains(html, "<dt>Phát hiện dấu vết</dt><dd>KHÔNG");
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
            var analysis = Analysis(
                CrackTraceVerdict.TraceDetected,
                new[] { check });
            analysis.Evidence = new[]
            {
                "registry-interference | account=private.user@example.com",
                "registry-interference | key=AAAAA-BBBBB-CCCCC-DDDDD-ABCDE"
            };
            var json = Write(
                new JsonAuditReportWriter(new AuditResultSanitizer()),
                "json",
                new AuditResult { CrackTraceAnalysis = analysis });

            Assert.IsFalse(json.Contains("AAAAA-BBBBB"));
            Assert.IsFalse(json.Contains("super-secret"));
            Assert.IsFalse(json.Contains("private.user@example.com"));
            Assert.IsFalse(json.Contains(@"\alice\"));
            StringAssert.Contains(json, "XXXXX-XXXXX-XXXXX-XXXXX-ABCDE");
            StringAssert.Contains(json, "[REDACTED]");
        }

        private static CrackTraceCheckResult Check(
            int order,
            CrackTraceStatus status)
        {
            return new CrackTraceCheckResult
            {
                Order = order,
                Id = "check-" + order,
                DisplayName = "Kiểm tra " + order,
                Status = status,
                Completed = status != CrackTraceStatus.Error &&
                            status != CrackTraceStatus.Unknown,
                Matched = status == CrackTraceStatus.Suspicious ||
                          status == CrackTraceStatus.Detected,
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
                ScanCompleted = true,
                ActivationDetected = true,
                TraceDetected = verdict == CrackTraceVerdict.TraceDetected,
                ProvenanceVerified = false,
                ActivationState = WindowsActivationState.Activated,
                TraceVerdict = verdict,
                ProvenanceVerdict = LicenseProvenanceVerdict.Unverified,
                DetectionCoverage = new[]
                {
                    new DetectionCoverageItem
                    {
                        Id = "current-licensing-state",
                        DisplayName = "Trạng thái cấp phép hiện tại",
                        Status = DetectionCoverageStatus.Checked,
                        Checked = true
                    },
                    new DetectionCoverageItem
                    {
                        Id = "digital-license-provenance",
                        DisplayName = "Nguồn gốc giấy phép số",
                        Status =
                            DetectionCoverageStatus.NotTechnicallyVerifiable,
                        Checked = false
                    }
                },
                BlindSpots = new[] { "chưa kiểm tra nguồn xác minh nguồn gốc" },
                Evidence = new[]
                {
                    "check-1 | Sổ đăng ký: NoGenTicket=1"
                },
                Confidence = 50,
                VerdictSummary = verdict == CrackTraceVerdict.TraceDetected
                    ? "CÓ DẤU VẾT"
                    : "KHÔNG PHÁT HIỆN DẤU VẾT",
                Checks = checks
            };
        }

        private static string Write(
            IAuditReportWriter writer,
            string extension,
            AuditResult audit)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid() + "." + extension);
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
