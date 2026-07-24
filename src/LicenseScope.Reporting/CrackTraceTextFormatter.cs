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
                    Text = Prefix(check.Status) + " " + check.Order + ". " +
                           check.DisplayName.PadRight(20) + ": " + check.Summary,
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
                Text = "ActivationState: " +
                       CrackTraceVerdictNames.ToMachineValue(analysis.ActivationState),
                IsHeading = true
            });
            lines.Add(new CrackTraceDisplayLine
            {
                Text = "TraceVerdict: " +
                       CrackTraceVerdictNames.ToMachineValue(analysis.TraceVerdict),
                Status = VerdictStatus(analysis.TraceVerdict)
            });
            lines.Add(new CrackTraceDisplayLine
            {
                Text = "ProvenanceVerdict: " +
                       CrackTraceVerdictNames.ToMachineValue(analysis.ProvenanceVerdict),
                Status = CrackTraceStatus.Unknown
            });
            lines.Add(new CrackTraceDisplayLine
            {
                Text = "Confidence: " + analysis.Confidence,
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
                           CoverageText(coverage.Status),
                    Status = CoverageLineStatus(coverage.Status)
                });
            }
            if (analysis.BlindSpots.Count > 0)
            {
                lines.Add(new CrackTraceDisplayLine());
                lines.Add(new CrackTraceDisplayLine
                {
                    Text = "Blind spots:",
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
                case CrackTraceStatus.TraceNotFound: return "[+]";
                case CrackTraceStatus.Suspicious: return "[!]";
                case CrackTraceStatus.Detected: return "[-]";
                case CrackTraceStatus.Unknown: return "[?]";
                default: return "[x]";
            }
        }

        private static CrackTraceStatus VerdictStatus(CrackTraceVerdict verdict)
        {
            switch (verdict)
            {
                case CrackTraceVerdict.TraceNotFound:
                    return CrackTraceStatus.TraceNotFound;
                case CrackTraceVerdict.Suspicious: return CrackTraceStatus.Suspicious;
                case CrackTraceVerdict.HighRisk: return CrackTraceStatus.Detected;
                case CrackTraceVerdict.Inconclusive: return CrackTraceStatus.Unknown;
                default: return CrackTraceStatus.Error;
            }
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

        private static CrackTraceStatus CoverageLineStatus(
            DetectionCoverageStatus status)
        {
            return status == DetectionCoverageStatus.Checked
                ? CrackTraceStatus.TraceNotFound
                : CrackTraceStatus.Unknown;
        }
    }
}
