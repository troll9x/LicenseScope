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
            return await AnalyzeAsync(
                    context,
                    new CrackTraceScanOptions(),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CrackTraceAnalysisResult> AnalyzeAsync(
            SystemContext context,
            CrackTraceScanOptions options,
            CancellationToken cancellationToken)
        {
            var started = DateTimeOffset.UtcNow;
            options = options ?? new CrackTraceScanOptions();
            try
            {
                var snapshot = await _source.CollectAsync(context, cancellationToken)
                    .ConfigureAwait(false);
                if (options.DeepForensicScan)
                {
                    if (!options.UserConsented)
                        throw new InvalidOperationException(
                            "Deep forensic scan requires explicit user consent.");
                    var deepSource = _source as IDeepCrackTraceEvidenceSource;
                    if (deepSource == null)
                    {
                        snapshot.UnavailableSources = snapshot.UnavailableSources
                            .Concat(new[] { "DeepForensic: source unavailable" })
                            .ToArray();
                    }
                    else
                    {
                        var deep = await deepSource
                            .CollectDeepForensicAsync(context, cancellationToken)
                            .ConfigureAwait(false);
                        MergeDeepEvidence(snapshot, deep);
                    }
                }
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
                var activationState = DetermineActivationState(snapshot.Activation);
                var provenanceVerdict = DetermineProvenance(snapshot.Activation);
                var verdict = CrackTraceVerdictEvaluator.Evaluate(
                    checks,
                    activationState,
                    provenanceVerdict);
                if (verdict == CrackTraceVerdict.TraceNotFound &&
                    snapshot.UnavailableSources.Count > 0)
                    verdict = CrackTraceVerdict.Inconclusive;
                return new CrackTraceAnalysisResult
                {
                    StartedAt = started,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Checks = checks,
                    ActivationState = activationState,
                    TraceVerdict = verdict,
                    ProvenanceVerdict = provenanceVerdict,
                    DetectionCoverage = BuildCoverage(snapshot, options),
                    BlindSpots = BuildBlindSpots(snapshot, options),
                    Evidence = BuildEvidence(snapshot, checks, activationState),
                    Confidence = CrackTraceVerdictEvaluator.Confidence(verdict, checks),
                    DeepForensicScanEnabled =
                        options.DeepForensicScan && snapshot.DeepForensicScanPerformed,
                    VerdictSummary = CrackTraceVerdictEvaluator.Summary(
                        verdict,
                        activationState)
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
                    ActivationState = WindowsActivationState.Unknown,
                    TraceVerdict = CrackTraceVerdict.ScanError,
                    ProvenanceVerdict = LicenseProvenanceVerdict.Inconclusive,
                    DetectionCoverage = ErrorCoverage(),
                    BlindSpots = new[]
                    {
                        "Scan failed before detection coverage could be established."
                    },
                    Evidence = Array.Empty<string>(),
                    Confidence = 0,
                    DeepForensicScanEnabled = false,
                    VerdictSummary = CrackTraceVerdictEvaluator.Summary(
                        CrackTraceVerdict.ScanError,
                        WindowsActivationState.Unknown)
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
                return Result(1, "kms-crack", "KMS Crack", CrackTraceStatus.TraceNotFound,
                    "Đã quan sát cấu hình KMS hiện tại; không tìm thấy artifact allowlist đi kèm. Điều này không xác minh nguồn gốc kích hoạt.",
                    evidence, 70);

            return Result(1, "kms-crack", "KMS Crack", CrackTraceStatus.TraceNotFound,
                "Không tìm thấy cấu hình KMS hoặc artifact KMS khớp allowlist trong phép kiểm tra hiện tại.", evidence, 60,
                incomplete: HasUnavailable(snapshot, "Activation"));
        }

        private static CrackTraceCheckResult AnalyzeMasHwid(CrackTraceEvidenceSnapshot snapshot)
        {
            var evidence = new List<string>();
            var weakArtifacts = snapshot.Paths.Concat(snapshot.Processes)
                .Concat(snapshot.Tasks)
                .Concat(snapshot.Events)
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
                evidence.Add(
                    "Permanent Retail/OEM activation observed; digital-license provenance is not technically verifiable from current state.");
            return Result(2, "mas-hwid", "MAS / HWID", CrackTraceStatus.TraceNotFound,
                "Không tìm thấy dấu vết MAS/HWID có thể kiểm chứng trong các nguồn hiện đã kiểm tra; trạng thái kích hoạt số không chứng minh nguồn gốc.",
                evidence, 45);
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
            return Result(3, "kms38-hook", "KMS38 Hook", CrackTraceStatus.TraceNotFound,
                "Không tìm thấy tổ hợp dấu vết KMS38 trong phạm vi kiểm tra hiện tại.", evidence, 55);
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
            if (!string.IsNullOrWhiteSpace(activation.ProductName))
                evidence.Add("Active edition: " +
                             SensitiveDataMasker.SanitizeDiagnosticText(activation.ProductName));
            if (!string.IsNullOrWhiteSpace(activation.FirmwareEdition))
                evidence.Add("Firmware edition: " +
                             SensitiveDataMasker.SanitizeDiagnosticText(activation.FirmwareEdition));
            evidence.Add("Relevant SoftwareLicensingProduct records: " +
                         activation.Products.Count);
            foreach (var product in activation.Products)
            {
                evidence.Add(
                    "Licensing record: " +
                    SensitiveDataMasker.SanitizeDiagnosticText(product.ProductName) +
                    "; channel=" + product.Channel +
                    "; status=" +
                    (product.LicenseStatus.HasValue
                        ? product.LicenseStatus.Value.ToString()
                        : "unknown") +
                    "; graceMinutes=" +
                    (product.GracePeriodRemaining.HasValue
                        ? product.GracePeriodRemaining.Value.ToString()
                        : "unknown") +
                    (string.IsNullOrWhiteSpace(product.KmsHost)
                        ? string.Empty
                        : "; kms=" + SanitizeHost(product.KmsHost) +
                          (product.KmsPort.HasValue
                              ? ":" + product.KmsPort.Value
                              : string.Empty)));
            }
            if (!string.IsNullOrWhiteSpace(activation.KmsHost))
                evidence.Add("Current KMS endpoint: " +
                             SanitizeHost(activation.KmsHost) +
                             (activation.KmsPort.HasValue
                                 ? ":" + activation.KmsPort.Value
                                 : string.Empty));
            if (activation.ExpirationDate.HasValue)
                evidence.Add("Current activation/KMS expiration: " +
                             activation.ExpirationDate.Value.ToString("o"));

            var contradictory = activation.LicenseStatus == 1 && activation.IndicatesUnlicensed;
            var editionMismatch =
                activation.OemFirmwareKeyPresent &&
                !string.IsNullOrWhiteSpace(activation.FirmwareEdition) &&
                !EditionsConsistent(
                    activation.ProductName,
                    activation.FirmwareEdition);
            var kmsPermanentWithoutLease =
                activation.Channel.Equals("Volume_KMSCLIENT", StringComparison.OrdinalIgnoreCase) &&
                activation.IsPermanent &&
                (!activation.GracePeriodRemaining.HasValue ||
                 activation.GracePeriodRemaining.Value == 0) &&
                !activation.ExpirationDate.HasValue;
            if (contradictory || kmsPermanentWithoutLease || editionMismatch)
                return Result(4, "license-logic", "Logic bản quyền", CrackTraceStatus.Suspicious,
                    "Kênh và trạng thái bản quyền có điểm chưa nhất quán; cần xác minh thêm.",
                    evidence, 65);

            if (!activation.LicenseStatus.HasValue || string.IsNullOrWhiteSpace(activation.Channel))
                return Result(4, "license-logic", "Logic bản quyền", CrackTraceStatus.Unknown,
                    "Không đủ dữ liệu để đối chiếu đầy đủ edition, channel và trạng thái.",
                    evidence, 25, incomplete: HasUnavailable(snapshot, "Activation"));

            return Result(4, "license-logic", "Logic bản quyền",
                CrackTraceStatus.TraceNotFound,
                "Trạng thái kích hoạt, edition và channel hiện tại không có mâu thuẫn rõ ràng; đây không phải bằng chứng xác minh nguồn gốc license.",
                evidence, 60);
        }

        private static CrackTraceCheckResult AnalyzeToolPaths(CrackTraceEvidenceSnapshot snapshot)
        {
            var evidence = snapshot.Paths.Select(DescribeArtifact).ToArray();
            var historical = snapshot.Events.Where(x =>
                    Contains(x, "KMSpico") ||
                    Contains(x, "KMSAuto") ||
                    Contains(x, "KMSELDI") ||
                    Contains(x, "Activation-Renewal") ||
                    Contains(x, "AAct"))
                .ToArray();
            evidence = evidence.Concat(historical.Select(DescribeArtifact)).ToArray();
            var explicitTools = snapshot.Paths.Where(x =>
                    !Contains(x, "GenuineTicket") &&
                    (Contains(x, "KMSpico") ||
                     Contains(x, "KMSAuto") ||
                     Contains(x, "KMSELDI") ||
                     Contains(x, "Activation-Renewal")))
                .ToArray();
            if (historical.Select(x => x.Source)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() >= 2)
                return Result(5, "tool-paths", "Thư mục công cụ",
                    CrackTraceStatus.Detected,
                    "Nhiều nguồn lịch sử độc lập cùng khớp allowlist công cụ kích hoạt.",
                    evidence, 82, strong: true);
            if (explicitTools.Length > 0)
                return Result(5, "tool-paths", "Thư mục công cụ", CrackTraceStatus.Detected,
                    "Phát hiện đường dẫn chính xác nằm trong allowlist công cụ kích hoạt của repository.",
                    evidence, 88, strong: true);
            if (historical.Length > 0)
                return Result(5, "tool-paths", "Thư mục công cụ",
                    CrackTraceStatus.Suspicious,
                    "Nguồn lịch sử allowlist có dấu vết thực thi cần xác minh; không đủ để tự kết luận HIGH_RISK.",
                    evidence, 55);
            if (snapshot.Paths.Any(x => Contains(x, "GenuineTicket")))
                return Result(5, "tool-paths", "Thư mục công cụ", CrackTraceStatus.Unknown,
                    "GenuineTicket tồn tại nhưng riêng artifact Microsoft này không đủ để kết luận.",
                    evidence, 30);
            return Result(5, "tool-paths", "Thư mục công cụ",
                CrackTraceStatus.TraceNotFound,
                "Không tìm thấy đường dẫn công cụ trong allowlist giới hạn.", evidence, 60,
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
            return Result(6, "scheduled-tasks", "Tác vụ ẩn (Task)",
                CrackTraceStatus.TraceNotFound,
                "Không tìm thấy tác vụ kích hoạt khớp allowlist.", evidence, 60);
        }

        private static CrackTraceCheckResult AnalyzeRegistry(CrackTraceEvidenceSnapshot snapshot)
        {
            if (HasUnavailable(snapshot, "Registry"))
                return Result(7, "registry-interference", "Can thiệp Registry",
                    CrackTraceStatus.Unknown,
                    "Không thể đọc đầy đủ khóa Software Protection Platform bằng quyền hiện tại.",
                    Unavailable(snapshot, "Registry"), 10, incomplete: true);

            var toolKeys = snapshot.RegistryValues.Where(x =>
                    x.Present &&
                    x.Name.Equals("KnownToolKey", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (toolKeys.Length > 0)
                return Result(7, "registry-interference", "Can thiệp Registry",
                    CrackTraceStatus.Detected,
                    "Tìm thấy khóa registry chính xác trong allowlist công cụ kích hoạt; riêng nhóm registry không thể tự tạo verdict HIGH_RISK.",
                    toolKeys.Select(x => "Registry key: " + x.Path),
                    75,
                    strong: true);

            var noGenTicket = snapshot.RegistryValues.FirstOrDefault(x =>
                x.Name.Equals("NoGenTicket", StringComparison.OrdinalIgnoreCase));
            if (noGenTicket != null && noGenTicket.Present)
                return Result(7, "registry-interference", "Can thiệp Registry",
                    CrackTraceStatus.Suspicious,
                    "NoGenTicket là tín hiệu registry đơn lẻ, không mang tính kết luận và không thể tự tạo verdict HIGH_RISK.",
                    new[]
                    {
                        "Registry value: " + noGenTicket.Path + "\\" + noGenTicket.Name +
                        "=" + SensitiveDataMasker.SanitizeDiagnosticText(noGenTicket.Value)
                    }, 20);
            return Result(7, "registry-interference", "Can thiệp Registry",
                CrackTraceStatus.TraceNotFound,
                "Không tìm thấy giá trị registry allowlist trong phép kiểm tra hiện tại.",
                Array.Empty<string>(), 55);
        }

        private static void MergeDeepEvidence(
            CrackTraceEvidenceSnapshot target,
            CrackTraceEvidenceSnapshot deep)
        {
            target.Events = (target.Events ?? Array.Empty<CrackTraceArtifact>())
                .Concat(deep.Events ?? Array.Empty<CrackTraceArtifact>())
                .ToArray();
            target.UnavailableSources =
                (target.UnavailableSources ?? Array.Empty<string>())
                .Concat(deep.UnavailableSources ?? Array.Empty<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            target.DeepForensicScanPerformed = deep.DeepForensicScanPerformed;
            target.DeepForensicSourcesChecked =
                deep.DeepForensicSourcesChecked ?? Array.Empty<string>();
        }

        private static WindowsActivationState DetermineActivationState(
            WindowsActivationTrace activation)
        {
            if (activation.LicenseStatus == 1 ||
                activation.Products.Any(x => x.LicenseStatus == 1) ||
                activation.IsPermanent && !activation.IndicatesUnlicensed)
                return WindowsActivationState.Activated;
            if (activation.LicenseStatus.HasValue ||
                activation.Products.Any(x => x.LicenseStatus.HasValue))
                return WindowsActivationState.NotActivated;
            return WindowsActivationState.Unknown;
        }

        private static LicenseProvenanceVerdict DetermineProvenance(
            WindowsActivationTrace activation)
        {
            if (DetermineActivationState(activation) != WindowsActivationState.Activated)
                return LicenseProvenanceVerdict.Inconclusive;
            if (activation.Channel.Equals(
                    "OEM_DM",
                    StringComparison.OrdinalIgnoreCase) &&
                activation.OemFirmwareKeyPresent &&
                EditionsConsistent(
                    activation.ProductName,
                    activation.FirmwareEdition))
                return LicenseProvenanceVerdict.ConsistentState;
            return LicenseProvenanceVerdict.Unverified;
        }

        private static IReadOnlyList<DetectionCoverageItem> BuildCoverage(
            CrackTraceEvidenceSnapshot snapshot,
            CrackTraceScanOptions options)
        {
            var currentActivation = HasUnavailable(snapshot, "Activation")
                ? DetectionCoverageStatus.Unknown
                : DetectionCoverageStatus.Checked;
            var currentKms = HasUnavailable(snapshot, "Activation")
                ? DetectionCoverageStatus.Unknown
                : DetectionCoverageStatus.Checked;
            var currentArtifacts =
                HasUnavailable(snapshot, "Service") ||
                HasUnavailable(snapshot, "Process") ||
                HasUnavailable(snapshot, "Path") ||
                HasUnavailable(snapshot, "ScheduledTask") ||
                HasUnavailable(snapshot, "Registry")
                    ? DetectionCoverageStatus.Unknown
                    : DetectionCoverageStatus.Checked;
            DetectionCoverageStatus historical;
            string historicalDetail;
            if (!options.DeepForensicScan)
            {
                historical = DetectionCoverageStatus.NotChecked;
                historicalDetail = "Deep forensic scan was not enabled.";
            }
            else if (!snapshot.DeepForensicScanPerformed ||
                     snapshot.UnavailableSources.Any(IsDeepUnavailable))
            {
                historical = DetectionCoverageStatus.Unknown;
                historicalDetail =
                    "One or more consented forensic sources were unavailable.";
            }
            else
            {
                historical = DetectionCoverageStatus.Checked;
                historicalDetail =
                    "Consented allowlisted historical sources were checked.";
            }
            return new[]
            {
                Coverage(
                    "current-licensing-state",
                    "Current licensing state",
                    currentActivation,
                    "All relevant SoftwareLicensingProduct records were requested."),
                Coverage(
                    "current-kms-configuration",
                    "Current KMS configuration",
                    currentKms,
                    "Current host, port, lease/grace and expiration were requested."),
                Coverage(
                    "known-services-tasks-files",
                    "Known services/tasks/files",
                    currentArtifacts,
                    "Only repository allowlisted current-state artifacts were checked."),
                Coverage(
                    "historical-execution-traces",
                    "Historical execution traces",
                    historical,
                    historicalDetail),
                Coverage(
                    "digital-license-provenance",
                    "Digital-license provenance",
                    DetectionCoverageStatus.NotTechnicallyVerifiable,
                    "Current Windows state cannot establish how a digital entitlement was created.")
            };
        }

        private static DetectionCoverageItem Coverage(
            string id,
            string name,
            DetectionCoverageStatus status,
            string detail)
        {
            return new DetectionCoverageItem
            {
                Id = id,
                DisplayName = name,
                Status = status,
                Detail = detail
            };
        }

        private static IReadOnlyList<DetectionCoverageItem> ErrorCoverage()
        {
            return new[]
            {
                Coverage(
                    "current-licensing-state",
                    "Current licensing state",
                    DetectionCoverageStatus.Unknown,
                    "Scan error."),
                Coverage(
                    "current-kms-configuration",
                    "Current KMS configuration",
                    DetectionCoverageStatus.Unknown,
                    "Scan error."),
                Coverage(
                    "known-services-tasks-files",
                    "Known services/tasks/files",
                    DetectionCoverageStatus.Unknown,
                    "Scan error."),
                Coverage(
                    "historical-execution-traces",
                    "Historical execution traces",
                    DetectionCoverageStatus.Unknown,
                    "Scan error."),
                Coverage(
                    "digital-license-provenance",
                    "Digital-license provenance",
                    DetectionCoverageStatus.NotTechnicallyVerifiable,
                    "Not technically verifiable.")
            };
        }

        private static IReadOnlyList<string> BuildBlindSpots(
            CrackTraceEvidenceSnapshot snapshot,
            CrackTraceScanOptions options)
        {
            var values = new List<string>
            {
                "Digital-license provenance cannot be established from current activation state."
            };
            if (!options.DeepForensicScan)
                values.Add(
                    "Historical licensing, PowerShell, Defender, Prefetch and Amcache traces were not checked.");
            values.AddRange(snapshot.UnavailableSources.Select(
                SensitiveDataMasker.SanitizeDiagnosticText));
            return values.Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IReadOnlyList<string> BuildEvidence(
            CrackTraceEvidenceSnapshot snapshot,
            IEnumerable<CrackTraceCheckResult> checks,
            WindowsActivationState activationState)
        {
            var values = new List<string>
            {
                "Activation state: " +
                CrackTraceVerdictNames.ToMachineValue(activationState)
            };
            if (!string.IsNullOrWhiteSpace(snapshot.Activation.Channel))
                values.Add("Activation channel: " + snapshot.Activation.Channel);
            values.AddRange(checks.SelectMany(x => x.Evidence));
            return values.Select(SensitiveDataMasker.SanitizeDiagnosticText)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool IsDeepUnavailable(string source)
        {
            return source.StartsWith("LicensingEventLog", StringComparison.OrdinalIgnoreCase) ||
                   source.StartsWith("PowerShellOperational", StringComparison.OrdinalIgnoreCase) ||
                   source.StartsWith("DefenderDetectionHistory", StringComparison.OrdinalIgnoreCase) ||
                   source.StartsWith("Prefetch", StringComparison.OrdinalIgnoreCase) ||
                   source.StartsWith("Amcache", StringComparison.OrdinalIgnoreCase) ||
                   source.StartsWith("DeepForensic", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EditionsConsistent(string activeEdition, string firmwareEdition)
        {
            if (string.IsNullOrWhiteSpace(activeEdition) ||
                string.IsNullOrWhiteSpace(firmwareEdition))
                return false;
            var active = EditionToken(activeEdition);
            var firmware = EditionToken(firmwareEdition);
            return active.Length > 0 &&
                   active.Equals(firmware, StringComparison.OrdinalIgnoreCase);
        }

        private static string EditionToken(string value)
        {
            var normalized = (value ?? string.Empty).ToUpperInvariant();
            foreach (var token in new[]
                     {
                         "ENTERPRISE", "EDUCATION", "PROFESSIONAL", "PRO",
                         "HOME", "CORE", "ULTIMATE", "STARTER"
                     })
            {
                if (normalized.Contains(token))
                    return token == "PROFESSIONAL" ? "PRO" :
                           token == "CORE" ? "HOME" : token;
            }
            return string.Empty;
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
            if (string.IsNullOrWhiteSpace(host)) return string.Empty;
            if (IsLoopback(host)) return "[loopback]";
            var publicMatch = WindowsCrackTraceEvidenceSource.PublicKmsKeywords
                .FirstOrDefault(x =>
                    host.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
            return publicMatch == null
                ? "[configured host]"
                : "[public-host keyword: " +
                  SensitiveDataMasker.SanitizeDiagnosticText(publicMatch) + "]";
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
        public static CrackTraceVerdict Evaluate(
            IEnumerable<CrackTraceCheckResult> checks,
            WindowsActivationState activationState,
            LicenseProvenanceVerdict provenanceVerdict)
        {
            var values = (checks ?? Array.Empty<CrackTraceCheckResult>()).ToArray();
            if (values.Length == 0 || values.Count(x => x.Status == CrackTraceStatus.Error) >= 4)
                return CrackTraceVerdict.ScanError;
            var detected = values.Where(x => x.Status == CrackTraceStatus.Detected).ToArray();
            if (detected.Where(x => x.IsStrongSignal)
                    .Select(x => x.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() >= 2)
                return CrackTraceVerdict.HighRisk;
            if (detected.Length > 0 || values.Any(x => x.Status == CrackTraceStatus.Suspicious))
                return CrackTraceVerdict.Suspicious;
            if (values.Any(x => x.IsDataIncomplete))
                return CrackTraceVerdict.Inconclusive;
            if (activationState == WindowsActivationState.Activated &&
                (provenanceVerdict == LicenseProvenanceVerdict.Unverified ||
                 provenanceVerdict == LicenseProvenanceVerdict.ConsistentState))
                return CrackTraceVerdict.Inconclusive;
            return CrackTraceVerdict.TraceNotFound;
        }

        public static int Confidence(
            CrackTraceVerdict verdict,
            IEnumerable<CrackTraceCheckResult> checks)
        {
            var values = (checks ?? Array.Empty<CrackTraceCheckResult>()).ToArray();
            if (verdict == CrackTraceVerdict.ScanError) return 0;
            if (verdict == CrackTraceVerdict.TraceNotFound) return 50;
            if (verdict == CrackTraceVerdict.Inconclusive) return 35;
            var signal = values.Where(x =>
                    x.Status == CrackTraceStatus.Detected ||
                    x.Status == CrackTraceStatus.Suspicious)
                .Select(x => x.Confidence)
                .DefaultIfEmpty(25)
                .Max();
            return verdict == CrackTraceVerdict.HighRisk
                ? Math.Max(80, signal)
                : Math.Min(75, signal);
        }

        public static string Summary(
            CrackTraceVerdict verdict,
            WindowsActivationState activationState)
        {
            switch (verdict)
            {
                case CrackTraceVerdict.TraceNotFound:
                    return "KHÔNG PHÁT HIỆN DẤU VẾT: Trong phạm vi các phép kiểm tra hiện tại, chưa tìm thấy dấu vết có thể kiểm chứng. Kết quả này không xác nhận nguồn gốc license hoặc chứng minh hệ thống chưa từng sử dụng công cụ kích hoạt.";
                case CrackTraceVerdict.Suspicious:
                    return "CẦN KIỂM TRA: Phát hiện một số dấu hiệu nghi vấn nhưng chưa đủ bằng chứng kết luận máy đang sử dụng công cụ kích hoạt trái phép.";
                case CrackTraceVerdict.HighRisk:
                    return "CẢNH BÁO NGUY CƠ CAO: Phát hiện nhiều dấu vết độc lập cho thấy hệ thống bản quyền có thể đã bị can thiệp.";
                case CrackTraceVerdict.Inconclusive:
                    return activationState == WindowsActivationState.Activated
                        ? "KHÔNG THỂ KẾT LUẬN: Windows đang được kích hoạt, nhưng trạng thái hiện tại không đủ để phân biệt license hợp lệ với một số phương pháp tạo digital entitlement."
                        : "KHÔNG THỂ KẾT LUẬN: Một số nguồn dữ liệu không thể đọc đầy đủ hoặc trạng thái kích hoạt chưa xác định.";
                default:
                    return "LỖI QUÉT: Không thể hoàn tất phân tích dấu vết.";
            }
        }
    }
}
