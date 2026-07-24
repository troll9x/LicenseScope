using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Application;
using LicenseScope.Compatibility;
using LicenseScope.Core.Models;
using LicenseScope.Reporting;

namespace LicenseScope.Cli
{
    public interface ICliConsole
    {
        void WriteLine(string value);
    }

    public interface IColorCliConsole : ICliConsole
    {
        void WriteLine(string value, ConsoleColor? color);
    }

    public sealed class SystemCliConsole : IColorCliConsole
    {
        public void WriteLine(string value) => Console.WriteLine(value);

        public void WriteLine(string value, ConsoleColor? color)
        {
            if (!color.HasValue || Console.IsOutputRedirected)
            {
                Console.WriteLine(value);
                return;
            }
            var previous = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color.Value;
                Console.WriteLine(value);
            }
            finally
            {
                Console.ForegroundColor = previous;
            }
        }
    }

    public static class Program
    {
        public static int Main(string[] args) =>
            Create().RunAsync(args, CancellationToken.None).GetAwaiter().GetResult();

        private static CliApplication Create()
        {
            var sanitizer = new AuditResultSanitizer();
            return new CliApplication(
                UnifiedAuditService.CreateProduction(),
                new IAuditReportWriter[]
                {
                    new JsonAuditReportWriter(sanitizer),
                    new CsvAuditReportWriter(sanitizer),
                    new HtmlAuditReportWriter(sanitizer)
                },
                new SystemCliConsole());
        }
    }

    public sealed partial class CliApplication
    {
        private readonly IUnifiedAuditService _audit;
        private readonly IReadOnlyDictionary<string, IAuditReportWriter> _writers;
        private readonly ICliConsole _console;

        public CliApplication(
            IUnifiedAuditService audit,
            IEnumerable<IAuditReportWriter> writers,
            ICliConsole console)
        {
            _audit = audit;
            _writers = writers.ToDictionary(x => x.FormatId, StringComparer.OrdinalIgnoreCase);
            _console = console;
        }

        public async Task<int> RunAsync(string[] args, CancellationToken token)
        {
            try
            {
                if (args != null && args.Length > 0 &&
                    string.Equals(args[0], "compatibility", StringComparison.OrdinalIgnoreCase))
                    return PrintCompatibility(args);
                var parsed = Parse(args ?? Array.Empty<string>());
                if (parsed.Help)
                {
                    PrintHelp();
                    return 0;
                }
                if (parsed.Version)
                {
                    _console.WriteLine(
                        typeof(CliApplication).Assembly.GetName().Version?.ToString() ??
                        "1.0.0.0");
                    return 0;
                }
                if (!parsed.Valid)
                {
                    _console.WriteLine("Lỗi: " + parsed.Error);
                    _console.WriteLine("Dùng --help để xem hướng dẫn.");
                    return 4;
                }

                var environment =
                    new RuntimeEnvironmentDetector(new WindowsArchitectureProbe()).Detect();
                var assessment =
                    new CompatibilityEvaluator().Evaluate(environment, CurrentPayload.Describe(environment));
                if (!assessment.CanRunAudit)
                {
                    foreach (var reason in assessment.BlockingReasons)
                        _console.WriteLine("Lỗi tương thích: " + reason);
                    return environment.InstalledNetFrameworkVersion < new Version(4, 8) ? 8 : 7;
                }

                var result = await _audit.RunAllAsync(
                    token,
                    null,
                    new CrackTraceScanOptions
                    {
                        DeepForensicScan = parsed.DeepForensicScan,
                        UserConsented = parsed.ForensicConsent
                    }).ConfigureAwait(false);
                var consoleSnapshot = new AuditResultSanitizer().CreateReportSnapshot(
                    result,
                    new ReportWriteOptions { IncludeEvidence = true, IncludeWarnings = false });
                if (!parsed.Quiet)
                {
                    foreach (var product in consoleSnapshot.Products)
                        _console.WriteLine(
                            product.ScannerId + " | " + product.ProductName + " | " +
                            StatusName(product.Status) + " | " + product.LicenseType);
                    if (consoleSnapshot.CrackTraceAnalysis != null)
                        PrintCrackTrace(consoleSnapshot.CrackTraceAnalysis);
                }

                foreach (var format in parsed.Formats)
                {
                    var writer = _writers[format];
                    var path = Path.Combine(
                        parsed.Output,
                        DefaultName(format, result.CompletedAt));
                    var written = await writer.WriteAsync(
                        result,
                        new ReportWriteOptions
                        {
                            OutputPath = path,
                            IncludeEvidence = parsed.IncludeEvidence,
                            IncludeWarnings = true,
                            IncludeMachineName = parsed.IncludeMachine,
                            Overwrite = parsed.Overwrite
                        },
                        token).ConfigureAwait(false);
                    if (!written.Success)
                    {
                        _console.WriteLine("Xuất báo cáo thất bại: " + written.ErrorMessage);
                        return 5;
                    }
                    if (!parsed.Quiet) _console.WriteLine("Đã lưu báo cáo: " + written.OutputPath);
                }
                return ExitCode(result);
            }
            catch (OperationCanceledException)
            {
                _console.WriteLine("Đã hủy kiểm tra.");
                return 6;
            }
            catch (Exception ex)
            {
                _console.WriteLine("Kiểm tra thất bại (" + ex.GetType().Name + ").");
                return 3;
            }
        }

        public static int ExitCode(AuditResult result)
        {
            if (result.WasCancelled) return 6;
            if (result.Products.Any(product =>
                    product.Status == LicenseStatus.Unlicensed ||
                    product.Status == LicenseStatus.Expired))
                return 1;
            if (result.ScannerExecutions.Any(execution => !execution.WasSuccessful) ||
                result.Products.Any(product =>
                    product.Status == LicenseStatus.Unknown ||
                    product.Status == LicenseStatus.Error ||
                    product.Status == LicenseStatus.NeedsSignIn ||
                    product.Status == LicenseStatus.NeedsOnlineVerification))
                return 2;
            return 0;
        }

        private void PrintCrackTrace(CrackTraceAnalysisResult analysis)
        {
            foreach (var line in new CrackTraceTextFormatter().Format(analysis))
            {
                var colorConsole = _console as IColorCliConsole;
                if (colorConsole == null)
                {
                    _console.WriteLine(line.Text);
                    continue;
                }
                ConsoleColor? color = null;
                if (line.Status == CrackTraceStatus.TraceNotFound) color = ConsoleColor.Cyan;
                else if (line.Status == CrackTraceStatus.Suspicious) color = ConsoleColor.Yellow;
                else if (line.Status == CrackTraceStatus.Detected) color = ConsoleColor.Red;
                else if (line.Status == CrackTraceStatus.Unknown) color = ConsoleColor.Gray;
                else if (line.Status == CrackTraceStatus.Error) color = ConsoleColor.DarkRed;
                else if (line.IsHeading) color = ConsoleColor.Cyan;
                colorConsole.WriteLine(line.Text, color);
            }
        }

        private static string DefaultName(string format, DateTimeOffset time) =>
            "LicenseScope-Audit-" + time.ToLocalTime().ToString("yyyyMMdd-HHmmss") + "." + format;

        private void PrintHelp()
        {
            _console.WriteLine("LicenseScope.Cli — kiểm tra giấy phép chỉ đọc");
            _console.WriteLine("Cách dùng: LicenseScope.Cli.exe audit --all [tùy chọn]");
            _console.WriteLine("       LicenseScope.Cli.exe compatibility [--json]");
            _console.WriteLine(
                "Tùy chọn: --format json,csv,html  --output <thư_mục>  " +
                "--include-evidence|--no-evidence  --include-machine-name  " +
                "--deep-forensic-scan --consent-forensic-read  " +
                "--overwrite  --quiet  --help  --version");
            _console.WriteLine(
                "Kiểm tra gồm bảy nhóm phân tích dấu vết công cụ kích hoạt Windows. " +
                "Các nhãn vẫn đọc được khi cửa sổ lệnh không hỗ trợ màu.");
            _console.WriteLine(
                "Thư mục đầu ra mặc định: .\\reports. Mặc định riêng tư không xuất " +
                "tên máy và che khóa/tài khoản.");
            _console.WriteLine(
                "Mã thoát: 0 tương thích/không có phát hiện giấy phép chặn, " +
                "1 không có giấy phép/đã hết hạn, 2 chưa hoàn tất, " +
                "3 lỗi nghiêm trọng, 4 lỗi đối số, 5 lỗi báo cáo, 6 đã hủy, " +
                "7 hệ điều hành/kiến trúc không được hỗ trợ, 8 thiếu framework, " +
                "9 chưa xác định tương thích.");
        }

        private static Args Parse(string[] args)
        {
            var result = new Args();
            if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
            {
                result.Help = true;
                return result;
            }
            if (args.Length == 1 && args[0] == "--version")
            {
                result.Version = true;
                return result;
            }
            if (args.Length < 2 ||
                !string.Equals(args[0], "audit", StringComparison.OrdinalIgnoreCase))
            {
                result.Error = "Cần dùng lệnh 'audit --all'.";
                return result;
            }
            if (args.Contains("--help"))
            {
                result.Help = true;
                return result;
            }
            if (!args.Contains("--all"))
            {
                result.Error = "Lệnh audit yêu cầu tùy chọn --all.";
                return result;
            }

            result.Valid = true;
            for (var index = 1; index < args.Length; index++)
            {
                var argument = args[index];
                if (argument == "--all") continue;
                if (argument == "--format")
                {
                    if (++index >= args.Length)
                    {
                        result.Valid = false;
                        result.Error = "Thiếu giá trị định dạng.";
                        break;
                    }
                    foreach (var format in args[index].Split(','))
                    {
                        if (format != "json" && format != "csv" && format != "html")
                        {
                            result.Valid = false;
                            result.Error = "Định dạng báo cáo không hợp lệ.";
                            break;
                        }
                        if (!result.Formats.Contains(format)) result.Formats.Add(format);
                    }
                }
                else if (argument == "--output")
                {
                    if (++index >= args.Length)
                    {
                        result.Valid = false;
                        result.Error = "Thiếu đường dẫn đầu ra.";
                        break;
                    }
                    result.Output = Path.GetFullPath(args[index]);
                }
                else if (argument == "--include-evidence") result.IncludeEvidence = true;
                else if (argument == "--no-evidence") result.IncludeEvidence = false;
                else if (argument == "--include-machine-name") result.IncludeMachine = true;
                else if (argument == "--deep-forensic-scan")
                    result.DeepForensicScan = true;
                else if (argument == "--consent-forensic-read")
                    result.ForensicConsent = true;
                else if (argument == "--overwrite") result.Overwrite = true;
                else if (argument == "--quiet") result.Quiet = true;
                else
                {
                    result.Valid = false;
                    result.Error = "Tùy chọn không xác định: " + argument;
                    break;
                }
            }
            if (result.Valid && result.DeepForensicScan && !result.ForensicConsent)
            {
                result.Valid = false;
                result.Error =
                    "--deep-forensic-scan yêu cầu --consent-forensic-read.";
            }
            if (result.Valid && result.ForensicConsent && !result.DeepForensicScan)
            {
                result.Valid = false;
                result.Error =
                    "--consent-forensic-read chỉ hợp lệ khi dùng cùng --deep-forensic-scan.";
            }
            return result;
        }

        private static string StatusName(LicenseStatus status)
        {
            switch (status)
            {
                case LicenseStatus.Licensed: return "Có giấy phép";
                case LicenseStatus.Unlicensed: return "Không có giấy phép";
                case LicenseStatus.Trial: return "Dùng thử";
                case LicenseStatus.GracePeriod: return "Thời gian ân hạn";
                case LicenseStatus.Expired: return "Đã hết hạn";
                case LicenseStatus.NeedsSignIn: return "Cần đăng nhập";
                case LicenseStatus.NeedsOnlineVerification: return "Cần xác minh trực tuyến";
                case LicenseStatus.NotInstalled: return "Chưa cài đặt";
                case LicenseStatus.Unsupported: return "Không được hỗ trợ";
                case LicenseStatus.Error: return "Lỗi";
                default: return "Không rõ";
            }
        }

        private sealed class Args
        {
            public bool Valid;
            public bool Help;
            public bool Version;
            public string Error = string.Empty;
            public List<string> Formats = new List<string>();
            public string Output = Path.GetFullPath("reports");
            public bool IncludeEvidence = true;
            public bool IncludeMachine;
            public bool DeepForensicScan;
            public bool ForensicConsent;
            public bool Overwrite;
            public bool Quiet;
        }
    }
}
