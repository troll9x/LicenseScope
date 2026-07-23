using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;
using LicenseScope.Core.Security;
using LicenseScope.Windows.Acquisition;
using LicenseScope.Windows.Classification;
using LicenseScope.Windows.Models;

namespace LicenseScope.Windows
{
    public sealed class WindowsCrackTraceAnalyzer : ICrackTraceAnalyzer
    {
        private readonly ICrackTraceEvidenceSource _source;

        public WindowsCrackTraceAnalyzer(ICrackTraceEvidenceSource source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public async Task<CrackTraceAnalysisResult> AnalyzeAsync(
            SystemContext context,
            CancellationToken cancellationToken)
        {
            var started = DateTimeOffset.UtcNow;
            try
            {
                var snapshot = await _source.CollectAsync(context, cancellationToken)
                    .ConfigureAwait(false);
                var checks = new[]
                {
                    AnalyzeKms(snapshot),
                    AnalyzeMasHwid(snapshot),
                    AnalyzeKms38(snapshot),
                    AnalyzeLicenseLogic(snapshot),
                    AnalyzeToolPaths(snapshot),
                    AnalyzeTasks(snapshot),
                    AnalyzeRegistry(snapshot)
                };
                var verdict = CrackTraceVerdictEvaluator.Evaluate(checks);
                return new CrackTraceAnalysisResult
                {
                    StartedAt = started,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Checks = checks,
                    Verdict = verdict,
                    VerdictSummary = CrackTraceVerdictEvaluator.Summary(verdict)
                };
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                var checks = Enumerable.Range(1, 7)
                    .Select(order => Result(
                        order,
                        Id(order),
                        Name(order),
                        CrackTraceStatus.Error,
                        "Không thể hoàn tất nhóm kiểm tra (" + ex.GetType().Name + ").",
                        Array.Empty<string>(),
                        0,
                        incomplete: true))
                    .ToArray();
                return new CrackTraceAnalysisResult
                {
                    StartedAt = started,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Checks = checks,
                    Verdict = CrackTraceVerdict.ScanError,
                    VerdictSummary = CrackTraceVerdictEvaluator.Summary(CrackTraceVerdict.ScanError)
                };
            }
        }

        private static CrackTraceCheckResult AnalyzeKms(CrackTraceEvidenceSnapshot snapshot)
        {
            var activation = snapshot.Activation;
            var evidence = new List<string>();
            var suspiciousHost = IsSuspiciousKmsHost(activation.KmsHost);
            var loopback = IsLoopback(activation.KmsHost);
            if (!string.IsNullOrWhiteSpace(activation.KmsHost))
                evidence.Add("KMS host configured: " + SanitizeHost(activation.KmsHost) +
                             (activation.KmsPort.HasValue ? ":" + activation.KmsPort.Value : string.Empty));

            var active = snapshot.Services.Concat(snapshot.Processes)
                .Concat(snapshot.Tasks.Where(x => x.ActionMatched))
                .Concat(snapshot.Events)
                .Where(IsKmsArtifact)
                .ToArray();
            evidence.AddRange(active.Select(DescribeArtifact));

            if ((suspiciousHost && active.Length > 0) || loopback && active.Length > 0 ||
                active.Select(x => x.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 2)
                return Result(1, "kms-crack", "KMS Crack", CrackTraceStatus.Detected,
                    "Phát hiện nhiều dấu vết KMS không phù hợp với cấu hình doanh nghiệp thông thường.",
                    evidence, 90, strong: true, definitive: loopback && active.Any(x =>
                        x.Source == "Service" || x.Source == "Process"));

            if (suspiciousHost || active.Length > 0)
                return Result(1, "kms-crack", "KMS Crack", CrackTraceStatus.Suspicious,
                    "Có dấu hiệu KMS cần xác minh với quản trị viên hệ thống; chưa đủ bằng chứng kết luận.",
                    evidence, 60);

            if (!string.IsNullOrWhiteSpace(activation.KmsHost))
                return Result(1, "kms-crack", "KMS Crack", CrackTraceStatus.Clean,
                    "Có cấu hình KMS nhưng không khớp allowlist công cụ hoặc máy chủ KMS công cộng.",
                    evidence, 70);

            return Result(1, "kms-crack", "KMS Crack", CrackTraceStatus.Clean,
                "Không phát hiện dấu vết cấu hình KMS đáng ngờ.", evidence, 75);
        }

        private static CrackTraceCheckResult AnalyzeMasHwid(CrackTraceEvidenceSnapshot snapshot)
        {
            var evidence = new List<string>();
            var weakArtifacts = snapshot.Paths.Concat(snapshot.Processes).Concat(snapshot.Tasks)
                .Where(x =>
                    Contains(x, "gatherosstate") ||
                    Contains(x, "clipup") ||
                    Contains(x, "GenuineTicket") ||
                    Contains(x, "Activation-Renewal"))
                .ToArray();
            evidence.AddRange(weakArtifacts.Select(DescribeArtifact));
            var renewalPath = snapshot.Paths.Any(x => Contains(x, "Activation-Renewal"));
            var renewalTask = snapshot.Tasks.Any(x =>
                Contains(x, "Activation-Renewal") && x.ActionMatched);
            if (renewalPath && renewalTask)
                return Result(2, "mas-hwid", "MAS / HWID", CrackTraceStatus.Detected,
                    "Phát hiện artifact và tác vụ gia hạn độc lập cùng khớp allowlist hiện có.",
                    evidence, 85, strong: true);

            if (weakArtifacts.Select(x => x.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 2)
                return Result(2, "mas-hwid", "MAS / HWID", CrackTraceStatus.Suspicious,
                    "Có nhiều artifact lịch sử nhưng chưa đủ để phân biệt với quy trình Windows hợp lệ.",
                    evidence, 55);

            if (weakArtifacts.Length > 0)
                return Result(2, "mas-hwid", "MAS / HWID", CrackTraceStatus.Unknown,
                    "Có artifact đơn lẻ không mang tính kết luận; Digital Entitlement không tự chứng minh MAS.",
                    evidence, 30);

            if (IsDigitalEntitlementPattern(snapshot.Activation))
                evidence.Add("Retail/OEM channel with permanent activation is consistent with Digital Entitlement.");
            return Result(2, "mas-hwid", "MAS / HWID", CrackTraceStatus.Clean,
                "Không phát hiện dấu vết MAS/HWID có thể kiểm chứng; trạng thái kích hoạt số không bị coi là bất thường.",
                evidence, 55);
        }

        private static CrackTraceCheckResult AnalyzeKms38(CrackTraceEvidenceSnapshot snapshot)
        {
            var activation = snapshot.Activation;
            var evidence = new List<string>();
            var expiryAnomaly = activation.ExpirationDate.HasValue &&
                                activation.ExpirationDate.Value.Year >= 2037 &&
                                activation.ExpirationDate.Value.Year <= 2039;
            var volumePermanent = activation.IsPermanent &&
                                  activation.Channel.Equals(
                                      "Volume_KMSCLIENT", StringComparison.OrdinalIgnoreCase);
            var artifact = snapshot.Paths.Any(x =>
                Contains(x, "GenuineTicket") || Contains(x, "Activation-Renewal")) ||
                           snapshot.Tasks.Any(x => Contains(x, "KMS38"));
            if (expiryAnomaly)
                evidence.Add("Activation expiration year: " +
                             activation.ExpirationDate!.Value.Year + " (date alone is not conclusive).");
            if (volumePermanent)
                evidence.Add("Volume_KMSCLIENT channel reports permanent activation.");
            if (artifact)
                evidence.Add("A second KMS38-related artifact matched the repository allowlist.");

            if (expiryAnomaly && volumePermanent && artifact)
                return Result(3, "kms38-hook", "KMS38 Hook", CrackTraceStatus.Detected,
                    "Kết hợp thời hạn, kênh và artifact tạo thành nhiều tín hiệu độc lập.",
                    evidence, 90, strong: true);
            if (expiryAnomaly || volumePermanent || artifact)
                return Result(3, "kms38-hook", "KMS38 Hook", CrackTraceStatus.Suspicious,
                    "Có dấu hiệu tương thích với KMS38 nhưng chưa đủ evidence kết hợp.",
                    evidence, 50);
            return Result(3, "kms38-hook", "KMS38 Hook", CrackTraceStatus.Clean,
                "Không phát hiện tổ hợp dấu vết KMS38.", evidence, 70);
        }

        private static CrackTraceCheckResult AnalyzeLicenseLogic(CrackTraceEvidenceSnapshot snapshot)
        {
            var activation = snapshot.Activation;
            var evidence = new List<string>();
            if (!string.IsNullOrWhiteSpace(activation.Channel))
                evidence.Add("Activation channel: " + activation.Channel);
            if (activation.LicenseStatus.HasValue)
                evidence.Add("License status code: " + activation.LicenseStatus.Value);
            evidence.Add("Activation ID: " +
                         (string.IsNullOrWhiteSpace(activation.ActivationId) ? "not available" : "present"));
            if (!string.IsNullOrWhiteSpace(activation.PartialProductKey))
                evidence.Add("Partial product key: " + activation.PartialProductKey);
            evidence.Add("OEM firmware evidence: " +
                         (activation.OemFirmwareKeyPresent ? "present" : "not observed"));

            var contradictory = activation.LicenseStatus == 1 && activation.IndicatesUnlicensed;
            var kmsPermanentWithoutLease =
                activation.Channel.Equals("Volume_KMSCLIENT", StringComparison.OrdinalIgnoreCase) &&
                activation.IsPermanent &&
                (!activation.GracePeriodRemaining.HasValue ||
                 activation.GracePeriodRemaining.Value == 0) &&
                !activation.ExpirationDate.HasValue;
            if (contradictory || kmsPermanentWithoutLease)
                return Result(4, "license-logic", "Logic bản quyền", CrackTraceStatus.Suspicious,
                    "Kênh và trạng thái bản quyền có điểm chưa nhất quán; cần xác minh thêm.",
                    evidence, 65);

            if (!activation.LicenseStatus.HasValue || string.IsNullOrWhiteSpace(activation.Channel))
                return Result(4, "license-logic", "Logic bản quyền", CrackTraceStatus.Unknown,
                    "Không đủ dữ liệu để đối chiếu đầy đủ edition, channel và trạng thái.",
                    evidence, 25, incomplete: HasUnavailable(snapshot, "Activation"));

            return Result(4, "license-logic", "Logic bản quyền", CrackTraceStatus.Clean,
                "Kênh cấp phép và trạng thái quan sát được không có mâu thuẫn rõ ràng.",
                evidence, 75);
        }

        private static CrackTraceCheckResult AnalyzeToolPaths(CrackTraceEvidenceSnapshot snapshot)
        {
            var evidence = snapshot.Paths.Select(DescribeArtifact).ToArray();
            var explicitTools = snapshot.Paths.Where(x =>
                    !Contains(x, "GenuineTicket") &&
                    (Contains(x, "KMSpico") ||
                     Contains(x, "KMSAuto") ||
                     Contains(x, "KMSELDI") ||
                     Contains(x, "Activation-Renewal")))
                .ToArray();
            if (explicitTools.Length > 0)
                return Result(5, "tool-paths", "Thư mục công cụ", CrackTraceStatus.Detected,
                    "Phát hiện đường dẫn chính xác nằm trong allowlist công cụ kích hoạt của repository.",
                    evidence, 88, strong: true);
            if (snapshot.Paths.Any(x => Contains(x, "GenuineTicket")))
                return Result(5, "tool-paths", "Thư mục công cụ", CrackTraceStatus.Unknown,
                    "GenuineTicket tồn tại nhưng riêng artifact Microsoft này không đủ để kết luận.",
                    evidence, 30);
            return Result(5, "tool-paths", "Thư mục công cụ", CrackTraceStatus.Clean,
                "Không phát hiện đường dẫn công cụ trong allowlist giới hạn.", evidence, 75,
                incomplete: HasUnavailable(snapshot, "Path"));
        }

        private static CrackTraceCheckResult AnalyzeTasks(CrackTraceEvidenceSnapshot snapshot)
        {
            var evidence = snapshot.Tasks.Select(DescribeArtifact).ToArray();
            if (snapshot.Tasks.Any(x => x.NameMatched && x.ActionMatched))
                return Result(6, "scheduled-tasks", "Tác vụ ẩn (Task)",
                    CrackTraceStatus.Detected,
                    "Tên/path và action của tác vụ cùng khớp pattern đã kiểm chứng.",
                    evidence, 88, strong: true, definitive: true);
            if (snapshot.Tasks.Count > 0)
                return Result(6, "scheduled-tasks", "Tác vụ ẩn (Task)",
                    CrackTraceStatus.Suspicious,
                    "Tên hoặc action của tác vụ khớp pattern, nhưng chưa đồng thời khớp cả hai.",
                    evidence, 50);
            if (HasUnavailable(snapshot, "ScheduledTask"))
                return Result(6, "scheduled-tasks", "Tác vụ ẩn (Task)",
                    CrackTraceStatus.Unknown,
                    "Không thể đọc đầy đủ Scheduled Tasks bằng quyền hiện tại.",
                    Unavailable(snapshot, "ScheduledTask"), 10, incomplete: true);
            return Result(6, "scheduled-tasks", "Tác vụ ẩn (Task)", CrackTraceStatus.Clean,
                "Không phát hiện tác vụ kích hoạt đáng ngờ.", evidence, 75);
        }

        private static CrackTraceCheckResult AnalyzeRegistry(CrackTraceEvidenceSnapshot snapshot)
        {
            if (HasUnavailable(snapshot, "Registry"))
                return Result(7, "registry-interference", "Can thiệp Registry",
                    CrackTraceStatus.Unknown,
                    "Không thể đọc đầy đủ khóa Software Protection Platform bằng quyền hiện tại.",
                    Unavailable(snapshot, "Registry"), 10, incomplete: true);

            var noGenTicket = snapshot.RegistryValues.FirstOrDefault(x =>
                x.Name.Equals("NoGenTicket", StringComparison.OrdinalIgnoreCase));
            if (noGenTicket != null && noGenTicket.Present)
                return Result(7, "registry-interference", "Can thiệp Registry",
                    CrackTraceStatus.Clean,
                    "NoGenTicket là policy doanh nghiệp được Microsoft tài liệu hóa; không phải bằng chứng crack.",
                    new[]
                    {
                        "Registry value: " + noGenTicket.Path + "\\" + noGenTicket.Name +
                        "=" + SensitiveDataMasker.SanitizeDiagnosticText(noGenTicket.Value)
                    }, 85);
            return Result(7, "registry-interference", "Can thiệp Registry",
                CrackTraceStatus.Clean,
                "Không phát hiện giá trị registry đã xác minh là dấu hiệu can thiệp.",
                Array.Empty<string>(), 70);
        }

        private static CrackTraceCheckResult Result(
            int order,
            string id,
            string name,
            CrackTraceStatus status,
            string summary,
            IEnumerable<string> evidence,
            int confidence,
            bool strong = false,
            bool definitive = false,
            bool incomplete = false)
        {
            return new CrackTraceCheckResult
            {
                Order = order,
                Id = id,
                DisplayName = name,
                Status = status,
                Summary = summary,
                Evidence = (evidence ?? Array.Empty<string>())
                    .Select(SensitiveDataMasker.SanitizeDiagnosticText)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                Confidence = Math.Max(0, Math.Min(100, confidence)),
                IsStrongSignal = strong,
                IsDefinitiveActiveSignal = definitive,
                IsDataIncomplete = incomplete
            };
        }

        private static bool IsKmsArtifact(CrackTraceArtifact artifact)
        {
            return new[]
            {
                "kms", "vlmcsd", "winkso", "activation-renewal", "aact"
            }.Any(keyword => Contains(artifact, keyword));
        }

        private static bool Contains(CrackTraceArtifact artifact, string value)
        {
            var text = artifact.Name + " " + artifact.Path + " " +
                       artifact.Action + " " + artifact.MatchedKeyword;
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsSuspiciousKmsHost(string host)
        {
            if (IsLoopback(host)) return true;
            return WindowsCrackTraceEvidenceSource.PublicKmsKeywords.Any(x =>
                (host ?? string.Empty).IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsLoopback(string host)
        {
            var value = (host ?? string.Empty).Trim();
            return value.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("::1", StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith("127.", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDigitalEntitlementPattern(WindowsActivationTrace activation)
        {
            return activation.IsPermanent &&
                   (activation.Channel.Equals("Retail", StringComparison.OrdinalIgnoreCase) ||
                    activation.Channel.StartsWith("OEM_", StringComparison.OrdinalIgnoreCase));
        }

        private static string DescribeArtifact(CrackTraceArtifact artifact)
        {
            var value = artifact.Source + ": " + artifact.Name;
            if (!string.IsNullOrWhiteSpace(artifact.Path)) value += " [" + artifact.Path + "]";
            if (!string.IsNullOrWhiteSpace(artifact.Action)) value += " -> " + artifact.Action;
            return value;
        }

        private static string SanitizeHost(string host)
        {
            return SensitiveDataMasker.SanitizeDiagnosticText(host ?? string.Empty);
        }

        private static bool HasUnavailable(CrackTraceEvidenceSnapshot snapshot, string source)
        {
            return snapshot.UnavailableSources.Any(x =>
                x.StartsWith(source, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<string> Unavailable(
            CrackTraceEvidenceSnapshot snapshot,
            string source)
        {
            return snapshot.UnavailableSources.Where(x =>
                x.StartsWith(source, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        private static string Id(int order)
        {
            return new[]
            {
                "", "kms-crack", "mas-hwid", "kms38-hook", "license-logic",
                "tool-paths", "scheduled-tasks", "registry-interference"
            }[order];
        }

        private static string Name(int order)
        {
            return new[]
            {
                "", "KMS Crack", "MAS / HWID", "KMS38 Hook", "Logic bản quyền",
                "Thư mục công cụ", "Tác vụ ẩn (Task)", "Can thiệp Registry"
            }[order];
        }
    }

    public static class CrackTraceVerdictEvaluator
    {
        public static CrackTraceVerdict Evaluate(IEnumerable<CrackTraceCheckResult> checks)
        {
            var values = (checks ?? Array.Empty<CrackTraceCheckResult>()).ToArray();
            if (values.Length == 0 || values.Count(x => x.Status == CrackTraceStatus.Error) >= 4)
                return CrackTraceVerdict.ScanError;
            var detected = values.Where(x => x.Status == CrackTraceStatus.Detected).ToArray();
            if (detected.Any(x => x.IsDefinitiveActiveSignal) ||
                detected.Count(x => x.IsStrongSignal) >= 2)
                return CrackTraceVerdict.HighRisk;
            if (detected.Length > 0 || values.Any(x => x.Status == CrackTraceStatus.Suspicious))
                return CrackTraceVerdict.Suspicious;
            if (values.Any(x => x.IsDataIncomplete))
                return CrackTraceVerdict.Inconclusive;
            return CrackTraceVerdict.Clean;
        }

        public static string Summary(CrackTraceVerdict verdict)
        {
            switch (verdict)
            {
                case CrackTraceVerdict.Clean:
                    return "AN TOÀN: Chưa phát hiện dấu vết can thiệp bản quyền đáng ngờ.";
                case CrackTraceVerdict.Suspicious:
                    return "CẦN KIỂM TRA: Phát hiện một số dấu hiệu nghi vấn nhưng chưa đủ bằng chứng kết luận máy đang sử dụng công cụ kích hoạt trái phép.";
                case CrackTraceVerdict.HighRisk:
                    return "CẢNH BÁO NGUY CƠ CAO: Phát hiện nhiều dấu vết độc lập cho thấy hệ thống bản quyền có thể đã bị can thiệp.";
                case CrackTraceVerdict.Inconclusive:
                    return "CHƯA THỂ KẾT LUẬN: Một số nguồn dữ liệu không thể đọc đầy đủ bằng quyền hiện tại.";
                default:
                    return "LỖI QUÉT: Không thể hoàn tất phân tích dấu vết.";
            }
        }
    }
}
