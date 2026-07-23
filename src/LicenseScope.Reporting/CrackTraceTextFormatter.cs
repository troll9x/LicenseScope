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
                Text = "[KẾT LUẬN ĐÁNH GIÁ WINDOWS]",
                IsHeading = true
            });
            lines.Add(new CrackTraceDisplayLine());
            lines.Add(new CrackTraceDisplayLine
            {
                Text = "=> " + analysis.VerdictSummary,
                Status = VerdictStatus(analysis.Verdict),
                IsVerdict = true
            });
            return lines;
        }

        public static string Prefix(CrackTraceStatus status)
        {
            switch (status)
            {
                case CrackTraceStatus.Clean: return "[+]";
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
                case CrackTraceVerdict.Clean: return CrackTraceStatus.Clean;
                case CrackTraceVerdict.Suspicious: return CrackTraceStatus.Suspicious;
                case CrackTraceVerdict.HighRisk: return CrackTraceStatus.Detected;
                case CrackTraceVerdict.Inconclusive: return CrackTraceStatus.Unknown;
                default: return CrackTraceStatus.Error;
            }
        }
    }
}
