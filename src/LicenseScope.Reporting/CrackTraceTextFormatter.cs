using System;
using System.Collections.Generic;
using System.Linq;
using LicenseScope.Core.Models;

namespace LicenseScope.Reporting
{
    public sealed class CrackTraceDisplayLine
    {
        public string Text { get; set; } = string.Empty;
        public CrackTraceStatus? Status { get; set; }
        public bool IsHeading { get; set; }
        public bool IsVerdict { get; set; }
    }

    public sealed class CrackTraceTextFormatter
    {
        public IReadOnlyList<CrackTraceDisplayLine> Format(CrackTraceAnalysisResult analysis)
        {
            if (analysis == null) throw new ArgumentNullException(nameof(analysis));
            var lines = new List<CrackTraceDisplayLine>
            {
                new CrackTraceDisplayLine
                {
                    Text = "[PHÂN TÍCH - DẤU VẾT CRACK]",
                    IsHeading = true
                },
                new CrackTraceDisplayLine()
            };
            foreach (var check in analysis.Checks.OrderBy(x => x.Order))
            {
                lines.Add(new CrackTraceDisplayLine
                {
                    Text = "[" + YesNo(check.Matched) + "] " + check.Order + ". " +
                           check.DisplayName.PadRight(20) +
                           " | Completed: " + YesNo(check.Completed) +
                           " | Matched: " + YesNo(check.Matched),
                    Status = check.Status
                });
                if (check.Evidence.Count > 0)
                {
                    lines.Add(new CrackTraceDisplayLine
                    {
                        Text = "    Evidence:",
                        Status = check.Status
                    });
                    foreach (var evidence in check.Evidence)
                        lines.Add(new CrackTraceDisplayLine
                        {
                            Text = "    - " + evidence,
                            Status = check.Status
                        });
                }
            }
            lines.Add(new CrackTraceDisplayLine());
            lines.Add(new CrackTraceDisplayLine
            {
                Text = "ScanCompleted: " + YesNo(analysis.ScanCompleted),
                IsHeading = true
            });
            lines.Add(new CrackTraceDisplayLine
            {
                Text = "ActivationDetected: " + YesNo(analysis.ActivationDetected),
                Status = VerdictStatus(analysis.TraceVerdict)
            });
            lines.Add(new CrackTraceDisplayLine
            {
                Text = "TraceDetected: " + YesNo(analysis.TraceDetected),
                Status = VerdictStatus(analysis.TraceVerdict)
            });
            lines.Add(new CrackTraceDisplayLine
            {
                Text = "ProvenanceVerified: " + YesNo(analysis.ProvenanceVerified),
                Status = VerdictStatus(analysis.TraceVerdict)
            });
            lines.Add(new CrackTraceDisplayLine());
            lines.Add(new CrackTraceDisplayLine
            {
                Text = "Coverage:",
                IsHeading = true
            });
            foreach (var coverage in analysis.DetectionCoverage)
            {
                lines.Add(new CrackTraceDisplayLine
                {
                    Text = "- " + coverage.DisplayName + ": " +
                           YesNo(coverage.Checked),
                    Status = CoverageLineStatus(coverage.Checked)
                });
            }
            if (analysis.BlindSpots.Count > 0)
            {
                lines.Add(new CrackTraceDisplayLine());
                lines.Add(new CrackTraceDisplayLine
                {
                    Text = "Unchecked source details:",
                    IsHeading = true
                });
                foreach (var blindSpot in analysis.BlindSpots)
                    lines.Add(new CrackTraceDisplayLine
                    {
                        Text = "- " + blindSpot,
                        Status = CrackTraceStatus.Unknown
                    });
            }
            if (analysis.Evidence.Count > 0)
            {
                lines.Add(new CrackTraceDisplayLine());
                lines.Add(new CrackTraceDisplayLine
                {
                    Text = "Evidence:",
                    IsHeading = true
                });
                foreach (var evidence in analysis.Evidence)
                    lines.Add(new CrackTraceDisplayLine
                    {
                        Text = "- " + evidence,
                        Status = CrackTraceStatus.Unknown
                    });
            }
            lines.Add(new CrackTraceDisplayLine());
            lines.Add(new CrackTraceDisplayLine
            {
                Text = "[KẾT LUẬN ĐÁNH GIÁ WINDOWS]",
                IsHeading = true
            });
            lines.Add(new CrackTraceDisplayLine());
            lines.Add(new CrackTraceDisplayLine
            {
                Text = "=> " + analysis.VerdictSummary,
                Status = VerdictStatus(analysis.TraceVerdict),
                IsVerdict = true
            });
            return lines;
        }

        public static string Prefix(CrackTraceStatus status)
        {
            switch (status)
            {
                case CrackTraceStatus.Suspicious:
                case CrackTraceStatus.Detected:
                    return "[CÓ]";
                default:
                    return "[KHÔNG]";
            }
        }

        private static CrackTraceStatus VerdictStatus(CrackTraceVerdict verdict)
        {
            switch (verdict)
            {
                case CrackTraceVerdict.TraceDetected:
                    return CrackTraceStatus.Detected;
                default:
                    return CrackTraceStatus.TraceNotFound;
            }
        }

        private static string YesNo(bool value)
        {
            return value ? "CÓ" : "KHÔNG";
        }

        private static CrackTraceStatus CoverageLineStatus(
            bool checkedSource)
        {
            return checkedSource
                ? CrackTraceStatus.TraceNotFound
                : CrackTraceStatus.Unknown;
        }
    }
}
