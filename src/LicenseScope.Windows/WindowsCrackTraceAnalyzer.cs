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
                            "Quét pháp chứng chuyên sâu cần sự đồng ý rõ ràng của người dùng.");
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
                var verdict = CrackTraceVerdictEvaluator.Evaluate(checks);
                var scanCompleted = checks.All(x => x.Completed) &&
                                    (!options.DeepForensicScan ||
                                     !snapshot.UnavailableSources.Any(
                                         IsDeepUnavailable));
                var traceDetected = verdict == CrackTraceVerdict.TraceDetected;
                var evidence = BuildEvidence(checks);
                return new CrackTraceAnalysisResult
                {
                    StartedAt = started,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Checks = checks,
                    ScanCompleted = scanCompleted,
                    ActivationDetected =
                        activationState == WindowsActivationState.Activated,
                    TraceDetected = traceDetected,
                    ProvenanceVerified = false,
                    ActivationState = activationState,
                    TraceVerdict = verdict,
                    ProvenanceVerdict = provenanceVerdict,
                    DetectionCoverage = BuildCoverage(snapshot, options),
                    BlindSpots = BuildBlindSpots(snapshot, options),
                    Evidence = evidence,
                    Confidence = CrackTraceVerdictEvaluator.Confidence(verdict, checks),
                    DeepForensicScanEnabled =
                        options.DeepForensicScan && snapshot.DeepForensicScanPerformed,
                    VerdictSummary = CrackTraceVerdictEvaluator.Summary(
                        traceDetected,
                        scanCompleted,
                        evidence.Count)
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
                    ScanCompleted = false,
                    ActivationDetected = false,
                    TraceDetected = false,
                    ProvenanceVerified = false,
                    ActivationState = WindowsActivationState.Unknown,
                    TraceVerdict = CrackTraceVerdict.TraceNotFound,
                    ProvenanceVerdict = LicenseProvenanceVerdict.Inconclusive,
                    DetectionCoverage = ErrorCoverage(),
                    BlindSpots = new[]
                    {
                        "Quét bị lỗi trước khi có thể xác định phạm vi kiểm tra."
                    },
                    Evidence = Array.Empty<string>(),
                    Confidence = 0,
                    DeepForensicScanEnabled = false,
                    VerdictSummary = CrackTraceVerdictEvaluator.Summary(
                        false,
                        false,
                        0)
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
                evidence.Add("Máy chủ KMS đã cấu hình: " + SanitizeHost(activation.KmsHost) +
                             (activation.KmsPort.HasValue ? ":" + activation.KmsPort.Value : string.Empty));

            var active = snapshot.Services.Concat(snapshot.Processes)
                .Concat(snapshot.Tasks.Where(x => x.ActionMatched))
                .Concat(snapshot.Events)
                .Where(IsKmsArtifact)
                .ToArray();
            evidence.AddRange(active.Select(DescribeArtifact));

            if ((suspiciousHost && active.Length > 0) || loopback && active.Length > 0 ||
                active.Select(x => x.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 2)
                return Result(1, "kms-crack", "Kích hoạt KMS trái phép", CrackTraceStatus.Detected,
                    "Phát hiện nhiều dấu vết KMS không phù hợp với cấu hình doanh nghiệp thông thường.",
                    evidence, 90, strong: true, definitive: loopback && active.Any(x =>
                        x.Source == "Service" || x.Source == "Process"));

            if (suspiciousHost || active.Length > 0)
                return Result(1, "kms-crack", "Kích hoạt KMS trái phép", CrackTraceStatus.Suspicious,
                    "Quy tắc KMS trong danh sách cho phép khớp ít nhất một máy chủ hoặc dấu vết.",
                    evidence, 60);

            if (!string.IsNullOrWhiteSpace(activation.KmsHost))
                return Result(1, "kms-crack", "Kích hoạt KMS trái phép", CrackTraceStatus.TraceNotFound,
                    "Đã quan sát cấu hình KMS hiện tại; không tìm thấy dấu vết đi kèm trong danh sách cho phép. Điều này không xác minh nguồn gốc kích hoạt.",
                    evidence, 70);

            return Result(1, "kms-crack", "Kích hoạt KMS trái phép", CrackTraceStatus.TraceNotFound,
                "Không tìm thấy cấu hình KMS hoặc dấu vết KMS khớp danh sách cho phép trong phép kiểm tra hiện tại.", evidence, 60,
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
                return Result(2, "mas-hwid", "Kích hoạt số MAS/HWID", CrackTraceStatus.Detected,
                    "Phát hiện dấu vết và tác vụ gia hạn độc lập cùng khớp danh sách cho phép hiện có.",
                    evidence, 85, strong: true);

            if (weakArtifacts.Select(x => x.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 2)
                return Result(2, "mas-hwid", "Kích hoạt số MAS/HWID", CrackTraceStatus.Suspicious,
                    "Dấu vết MAS/HWID khớp danh sách cho phép ở ít nhất hai nguồn.",
                    evidence, 55);

            if (weakArtifacts.Length > 0)
                return Result(2, "mas-hwid", "Kích hoạt số MAS/HWID", CrackTraceStatus.Suspicious,
                    "Có ít nhất một dấu vết MAS/HWID khớp danh sách cho phép.",
                    evidence, 30);

            if (IsDigitalEntitlementPattern(snapshot.Activation))
                evidence.Add(
                    "Đã quan sát kích hoạt Retail/OEM vĩnh viễn; trạng thái hiện tại không cho phép xác minh nguồn gốc giấy phép số.");
            return Result(2, "mas-hwid", "Kích hoạt số MAS/HWID", CrackTraceStatus.TraceNotFound,
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
                evidence.Add("Năm hết hạn kích hoạt: " +
                             activation.ExpirationDate!.Value.Year + " (riêng ngày này không đủ để kết luận).");
            if (volumePermanent)
                evidence.Add("Kênh Volume_KMSCLIENT báo trạng thái kích hoạt vĩnh viễn.");
            if (artifact)
                evidence.Add("Một dấu vết thứ hai liên quan đến KMS38 khớp danh sách cho phép.");

            if (expiryAnomaly && volumePermanent && artifact)
                return Result(3, "kms38-hook", "Can thiệp KMS38", CrackTraceStatus.Detected,
                    "Kết hợp thời hạn, kênh và dấu vết tạo thành nhiều tín hiệu độc lập.",
                    evidence, 90, strong: true);
            if (expiryAnomaly || volumePermanent || artifact)
                return Result(3, "kms38-hook", "Can thiệp KMS38", CrackTraceStatus.Suspicious,
                    "Có ít nhất một quy tắc KMS38 khớp dữ liệu quan sát được.",
                    evidence, 50);
            return Result(3, "kms38-hook", "Can thiệp KMS38", CrackTraceStatus.TraceNotFound,
                "Không tìm thấy tổ hợp dấu vết KMS38 trong phạm vi kiểm tra hiện tại.", evidence, 55);
        }

        private static CrackTraceCheckResult AnalyzeLicenseLogic(CrackTraceEvidenceSnapshot snapshot)
        {
            var activation = snapshot.Activation;
            var evidence = new List<string>();
            if (!string.IsNullOrWhiteSpace(activation.Channel))
                evidence.Add("Kênh kích hoạt: " + activation.Channel);
            if (activation.LicenseStatus.HasValue)
                evidence.Add("Mã trạng thái giấy phép: " + activation.LicenseStatus.Value);
            evidence.Add("Mã kích hoạt: " +
                         (string.IsNullOrWhiteSpace(activation.ActivationId) ? "không có dữ liệu" : "có"));
            if (!string.IsNullOrWhiteSpace(activation.PartialProductKey))
                evidence.Add("Năm ký tự cuối khóa sản phẩm: " + activation.PartialProductKey);
            evidence.Add("Bằng chứng OEM trong firmware: " +
                         (activation.OemFirmwareKeyPresent ? "có" : "không quan sát thấy"));
            if (!string.IsNullOrWhiteSpace(activation.ProductName))
                evidence.Add("Phiên bản Windows đang kích hoạt: " +
                             SensitiveDataMasker.SanitizeDiagnosticText(activation.ProductName));
            if (!string.IsNullOrWhiteSpace(activation.FirmwareEdition))
                evidence.Add("Phiên bản Windows trong firmware: " +
                             SensitiveDataMasker.SanitizeDiagnosticText(activation.FirmwareEdition));
            evidence.Add("Số bản ghi SoftwareLicensingProduct liên quan: " +
                         activation.Products.Count);
            foreach (var product in activation.Products)
            {
                evidence.Add(
                    "Bản ghi cấp phép: " +
                    SensitiveDataMasker.SanitizeDiagnosticText(product.ProductName) +
                    "; kênh=" + product.Channel +
                    "; trạng thái=" +
                    (product.LicenseStatus.HasValue
                        ? product.LicenseStatus.Value.ToString()
                        : "không rõ") +
                    "; phút ân hạn=" +
                    (product.GracePeriodRemaining.HasValue
                        ? product.GracePeriodRemaining.Value.ToString()
                        : "không rõ") +
                    (string.IsNullOrWhiteSpace(product.KmsHost)
                        ? string.Empty
                        : "; kms=" + SanitizeHost(product.KmsHost) +
                          (product.KmsPort.HasValue
                              ? ":" + product.KmsPort.Value
                              : string.Empty)));
            }
            if (!string.IsNullOrWhiteSpace(activation.KmsHost))
                evidence.Add("Điểm cuối KMS hiện tại: " +
                             SanitizeHost(activation.KmsHost) +
                             (activation.KmsPort.HasValue
                                 ? ":" + activation.KmsPort.Value
                                 : string.Empty));
            if (activation.ExpirationDate.HasValue)
                evidence.Add("Thời điểm hết hạn kích hoạt/KMS hiện tại: " +
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
                    "Có ít nhất một quy tắc đối chiếu phiên bản, kênh và trạng thái không khớp.",
                    evidence, 65);

            if (!activation.LicenseStatus.HasValue || string.IsNullOrWhiteSpace(activation.Channel))
                return Result(4, "license-logic", "Logic bản quyền", CrackTraceStatus.Unknown,
                    "Không đủ dữ liệu để đối chiếu đầy đủ phiên bản, kênh và trạng thái.",
                    evidence, 25, incomplete: HasUnavailable(snapshot, "Activation"));

            return Result(4, "license-logic", "Logic bản quyền",
                CrackTraceStatus.TraceNotFound,
                "Trạng thái kích hoạt, phiên bản và kênh hiện tại không có mâu thuẫn rõ ràng; đây không phải bằng chứng xác minh nguồn gốc giấy phép.",
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
                    "Nhiều nguồn lịch sử độc lập cùng khớp danh sách cho phép về công cụ kích hoạt.",
                    evidence, 82, strong: true);
            if (explicitTools.Length > 0)
                return Result(5, "tool-paths", "Thư mục công cụ", CrackTraceStatus.Detected,
                    "Phát hiện đường dẫn chính xác nằm trong danh sách cho phép về công cụ kích hoạt.",
                    evidence, 88, strong: true);
            if (historical.Length > 0)
                return Result(5, "tool-paths", "Thư mục công cụ",
                    CrackTraceStatus.Suspicious,
                    "Có ít nhất một dấu vết thực thi lịch sử khớp danh sách cho phép.",
                    evidence, 55);
            if (snapshot.Paths.Any(x => Contains(x, "GenuineTicket")))
                return Result(5, "tool-paths", "Thư mục công cụ", CrackTraceStatus.Suspicious,
                    "Có dấu vết GenuineTicket khớp quy tắc quan sát.",
                    evidence, 30);
            return Result(5, "tool-paths", "Thư mục công cụ",
                CrackTraceStatus.TraceNotFound,
                "Không tìm thấy đường dẫn công cụ trong danh sách cho phép giới hạn.", evidence, 60,
                incomplete: HasUnavailable(snapshot, "Path"));
        }

        private static CrackTraceCheckResult AnalyzeTasks(CrackTraceEvidenceSnapshot snapshot)
        {
            var evidence = snapshot.Tasks.Select(DescribeArtifact).ToArray();
            if (snapshot.Tasks.Any(x => x.NameMatched && x.ActionMatched))
                return Result(6, "scheduled-tasks", "Tác vụ ẩn",
                    CrackTraceStatus.Detected,
                    "Tên/path và action của tác vụ cùng khớp pattern đã kiểm chứng.",
                    evidence, 88, strong: true, definitive: true);
            if (snapshot.Tasks.Count > 0)
                return Result(6, "scheduled-tasks", "Tác vụ ẩn",
                    CrackTraceStatus.Suspicious,
                    "Tên hoặc action của tác vụ khớp pattern, nhưng chưa đồng thời khớp cả hai.",
                    evidence, 50);
            if (HasUnavailable(snapshot, "ScheduledTask"))
                return Result(6, "scheduled-tasks", "Tác vụ ẩn",
                    CrackTraceStatus.Unknown,
                    "Không thể đọc đầy đủ tác vụ đã lập lịch bằng quyền hiện tại.",
                    Unavailable(snapshot, "ScheduledTask"), 10, incomplete: true);
            return Result(6, "scheduled-tasks", "Tác vụ ẩn",
                CrackTraceStatus.TraceNotFound,
                "Không tìm thấy tác vụ kích hoạt khớp danh sách cho phép.", evidence, 60);
        }

        private static CrackTraceCheckResult AnalyzeRegistry(CrackTraceEvidenceSnapshot snapshot)
        {
            if (HasUnavailable(snapshot, "Registry"))
                return Result(7, "registry-interference", "Can thiệp sổ đăng ký",
                    CrackTraceStatus.Unknown,
                    "Không thể đọc đầy đủ khóa Software Protection Platform bằng quyền hiện tại.",
                    Unavailable(snapshot, "Registry"), 10, incomplete: true);

            var toolKeys = snapshot.RegistryValues.Where(x =>
                    x.Present &&
                    x.Name.Equals("KnownToolKey", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (toolKeys.Length > 0)
                return Result(7, "registry-interference", "Can thiệp sổ đăng ký",
                    CrackTraceStatus.Detected,
                    "Tìm thấy khóa sổ đăng ký chính xác trong danh sách cho phép về công cụ kích hoạt.",
                    toolKeys.Select(x => "Khóa sổ đăng ký: " + x.Path),
                    75,
                    strong: true);

            var noGenTicket = snapshot.RegistryValues.FirstOrDefault(x =>
                x.Name.Equals("NoGenTicket", StringComparison.OrdinalIgnoreCase));
            if (noGenTicket != null && noGenTicket.Present)
                return Result(7, "registry-interference", "Can thiệp sổ đăng ký",
                    CrackTraceStatus.Suspicious,
                    "Tìm thấy giá trị NoGenTicket tại registry path được kiểm tra.",
                    new[]
                    {
                        "Giá trị sổ đăng ký: " + noGenTicket.Path + "\\" + noGenTicket.Name +
                        "=" + SensitiveDataMasker.SanitizeDiagnosticText(noGenTicket.Value)
                    }, 20);
            return Result(7, "registry-interference", "Can thiệp sổ đăng ký",
                CrackTraceStatus.TraceNotFound,
                "Không tìm thấy giá trị sổ đăng ký thuộc danh sách cho phép trong phép kiểm tra hiện tại.",
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
                historicalDetail = "Chưa bật quét pháp chứng chuyên sâu.";
            }
            else if (!snapshot.DeepForensicScanPerformed ||
                     snapshot.UnavailableSources.Any(IsDeepUnavailable))
            {
                historical = DetectionCoverageStatus.Unknown;
                historicalDetail =
                    "Một hoặc nhiều nguồn pháp chứng đã được cho phép không khả dụng.";
            }
            else
            {
                historical = DetectionCoverageStatus.Checked;
                historicalDetail =
                    "Đã kiểm tra các nguồn lịch sử trong danh sách cho phép.";
            }
            return new[]
            {
                Coverage(
                    "current-licensing-state",
                    "Trạng thái cấp phép hiện tại",
                    currentActivation,
                    "Đã yêu cầu tất cả bản ghi SoftwareLicensingProduct liên quan."),
                Coverage(
                    "current-kms-configuration",
                    "Cấu hình KMS hiện tại",
                    currentKms,
                    "Đã yêu cầu máy chủ, cổng, thời gian thuê/ân hạn và thời điểm hết hạn hiện tại."),
                Coverage(
                    "known-services-tasks-files",
                    "Dịch vụ, tác vụ và tệp đã biết",
                    currentArtifacts,
                    "Chỉ kiểm tra các dấu vết trạng thái hiện tại nằm trong danh sách cho phép."),
                Coverage(
                    "historical-execution-traces",
                    "Dấu vết thực thi trong quá khứ",
                    historical,
                    historicalDetail),
                Coverage(
                    "digital-license-provenance",
                    "Nguồn gốc giấy phép số",
                    DetectionCoverageStatus.NotTechnicallyVerifiable,
                    "Trạng thái Windows hiện tại không thể xác định giấy phép số đã được tạo bằng cách nào.")
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
                Checked = status == DetectionCoverageStatus.Checked,
                Detail = detail
            };
        }

        private static IReadOnlyList<DetectionCoverageItem> ErrorCoverage()
        {
            return new[]
            {
                Coverage(
                    "current-licensing-state",
                    "Trạng thái cấp phép hiện tại",
                    DetectionCoverageStatus.Unknown,
                    "Lỗi quét."),
                Coverage(
                    "current-kms-configuration",
                    "Cấu hình KMS hiện tại",
                    DetectionCoverageStatus.Unknown,
                    "Lỗi quét."),
                Coverage(
                    "known-services-tasks-files",
                    "Dịch vụ, tác vụ và tệp đã biết",
                    DetectionCoverageStatus.Unknown,
                    "Lỗi quét."),
                Coverage(
                    "historical-execution-traces",
                    "Dấu vết thực thi trong quá khứ",
                    DetectionCoverageStatus.Unknown,
                    "Lỗi quét."),
                Coverage(
                    "digital-license-provenance",
                    "Nguồn gốc giấy phép số",
                    DetectionCoverageStatus.NotTechnicallyVerifiable,
                    "Không thể xác minh bằng kỹ thuật hiện có.")
            };
        }

        private static IReadOnlyList<string> BuildBlindSpots(
            CrackTraceEvidenceSnapshot snapshot,
            CrackTraceScanOptions options)
        {
            var values = new List<string>
            {
                "Không thể xác định nguồn gốc giấy phép số chỉ từ trạng thái kích hoạt hiện tại."
            };
            if (!options.DeepForensicScan)
                values.Add(
                    "Chưa kiểm tra lịch sử cấp phép, PowerShell, Defender, Prefetch và Amcache.");
            values.AddRange(snapshot.UnavailableSources.Select(
                DescribeUnavailableSource));
            return values.Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IReadOnlyList<string> BuildEvidence(
            IEnumerable<CrackTraceCheckResult> checks)
        {
            var values = (checks ?? Array.Empty<CrackTraceCheckResult>())
                .Where(x => x.Matched)
                .SelectMany(check => check.Evidence.Select(
                    value => check.Id + " | " + value));
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
                Completed = status != CrackTraceStatus.Error && !incomplete,
                Matched = status == CrackTraceStatus.Suspicious ||
                          status == CrackTraceStatus.Detected,
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
            var value = "Nguồn " + SourceName(artifact.Source) + ": " + artifact.Name;
            if (!string.IsNullOrWhiteSpace(artifact.Path)) value += " [" + artifact.Path + "]";
            if (!string.IsNullOrWhiteSpace(artifact.Action)) value += " -> hành động: " + artifact.Action;
            return value;
        }

        private static string SourceName(string source)
        {
            switch (source)
            {
                case "Service": return "dịch vụ";
                case "Process": return "tiến trình";
                case "ScheduledTask": return "tác vụ đã lập lịch";
                case "Path": return "đường dẫn";
                case "Registry": return "sổ đăng ký";
                case "Activation": return "trạng thái kích hoạt";
                case "DeepForensic": return "pháp chứng chuyên sâu";
                case "LicensingEventLog": return "nhật ký cấp phép";
                case "PowerShellOperational": return "nhật ký vận hành PowerShell";
                case "DefenderDetectionHistory": return "lịch sử phát hiện Defender";
                case "Prefetch": return "Prefetch";
                case "Amcache": return "Amcache";
                default: return source;
            }
        }

        private static string DescribeUnavailableSource(string value)
        {
            var sanitized = SensitiveDataMasker.SanitizeDiagnosticText(value);
            var separator = sanitized.IndexOf(':');
            if (separator < 0) return sanitized;
            var source = SourceName(sanitized.Substring(0, separator).Trim());
            var detail = sanitized.Substring(separator + 1).Trim()
                .Replace(
                    "allowlisted inventory is not mounted",
                    "kho kiểm kê trong danh sách cho phép chưa được gắn")
                .Replace(
                    "allowlisted lookup unavailable without enumerating unrelated entries",
                    "không thể tra cứu danh sách cho phép nếu không duyệt các mục không liên quan")
                .Replace("source unavailable", "nguồn dữ liệu không khả dụng")
                .Replace("query unavailable", "truy vấn không khả dụng")
                .Replace("access denied", "bị từ chối truy cập")
                .Replace("unavailable", "không khả dụng");
            return source + ": " + detail;
        }

        private static string SanitizeHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host)) return string.Empty;
            if (IsLoopback(host)) return "[máy cục bộ]";
            var publicMatch = WindowsCrackTraceEvidenceSource.PublicKmsKeywords
                .FirstOrDefault(x =>
                    host.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
            return publicMatch == null
                ? "[máy chủ đã cấu hình]"
                : "[từ khóa máy chủ công cộng: " +
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
                    x.StartsWith(source, StringComparison.OrdinalIgnoreCase))
                .Select(DescribeUnavailableSource)
                .ToArray();
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
                "", "Kích hoạt KMS trái phép", "Kích hoạt số MAS/HWID",
                "Can thiệp KMS38", "Logic bản quyền", "Thư mục công cụ",
                "Tác vụ ẩn", "Can thiệp sổ đăng ký"
            }[order];
        }
    }

    public static class CrackTraceVerdictEvaluator
    {
        public static CrackTraceVerdict Evaluate(
            IEnumerable<CrackTraceCheckResult> checks)
        {
            var values = (checks ?? Array.Empty<CrackTraceCheckResult>()).ToArray();
            return values.Any(x =>
                    x.Status == CrackTraceStatus.Detected ||
                    x.Status == CrackTraceStatus.Suspicious)
                ? CrackTraceVerdict.TraceDetected
                : CrackTraceVerdict.TraceNotFound;
        }

        public static int Confidence(
            CrackTraceVerdict verdict,
            IEnumerable<CrackTraceCheckResult> checks)
        {
            var values = (checks ?? Array.Empty<CrackTraceCheckResult>()).ToArray();
            if (verdict == CrackTraceVerdict.TraceNotFound) return 0;
            var signal = values.Where(x =>
                    x.Status == CrackTraceStatus.Detected ||
                    x.Status == CrackTraceStatus.Suspicious)
                .Select(x => x.Confidence)
                .DefaultIfEmpty(25)
                .Max();
            return signal;
        }

        public static string Summary(
            bool traceDetected,
            bool scanCompleted,
            int evidenceCount)
        {
            if (traceDetected)
                return "CÓ DẤU VẾT: Phát hiện " + evidenceCount +
                       " bằng chứng khớp danh sách cho phép. Xem mục Bằng chứng để đối chiếu nguồn và giá trị cụ thể.";
            return scanCompleted
                ? "KHÔNG PHÁT HIỆN DẤU VẾT: Không có bằng chứng nào khớp danh sách cho phép trong các nguồn đã kiểm tra."
                : "KHÔNG PHÁT HIỆN DẤU VẾT TRONG CÁC PHÉP KIỂM TRA HOÀN TẤT. Quét hoàn tất: KHÔNG.";
        }
    }
}
