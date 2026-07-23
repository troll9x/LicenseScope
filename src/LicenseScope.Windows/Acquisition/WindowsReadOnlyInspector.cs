using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;
using LicenseScope.Core.Runtime;
using LicenseScope.Windows.Models;

namespace LicenseScope.Windows.Acquisition
{
    public sealed class WindowsReadOnlyInspector
    {
        private readonly IProcessRunner _processRunner;
        private readonly WindowsInspectionSettings _settings;

        public WindowsReadOnlyInspector(IProcessRunner processRunner, WindowsInspectionSettings settings)
        {
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<WindowsInspectionResult> InspectAsync(
            SystemContext context,
            WindowsLicenseProductRecord? product,
            CancellationToken cancellationToken)
        {
            var evidence = new List<ScanEvidence>();
            var warnings = new List<string>();
            InspectKmsHost(product, evidence, warnings);
            InspectListeningPorts(evidence, warnings);
            InspectServices(evidence, warnings, cancellationToken);
            InspectProcesses(evidence, warnings, cancellationToken);
            InspectPaths(evidence, warnings, cancellationToken);
            await InspectScheduledTasksAsync(context, evidence, warnings, cancellationToken).ConfigureAwait(false);
            evidence.Add(new ScanEvidence
            {
                Source = "Cài đặt kiểm tra",
                Name = "ReadOnlyInspection",
                Value = warnings.Count == 0 ? "Không phát hiện chỉ dấu theo cấu hình" : warnings.Count + " chỉ dấu cần xem xét",
                Confidence = warnings.Count == 0 ? ConfidenceLevel.Low : ConfidenceLevel.Medium
            });
            return new WindowsInspectionResult { Evidence = evidence, Warnings = warnings };
        }

        private void InspectKmsHost(WindowsLicenseProductRecord? product, ICollection<ScanEvidence> evidence, ICollection<string> warnings)
        {
            var host = product?.KmsMachineName ?? string.Empty;
            var match = FindMatch(host, _settings.KmsDomainKeywords);
            if (match.Length == 0) return;
            evidence.Add(Fact("WMI", "ConfiguredKmsDomainMatch", match));
            warnings.Add("Cài đặt kiểm tra: máy chủ KMS khớp từ khóa miền cảnh báo '" + match + "'.");
        }

        private void InspectListeningPorts(ICollection<ScanEvidence> evidence, ICollection<string> warnings)
        {
            if (_settings.LocalPorts.Count == 0) return;
            try
            {
                var listening = new HashSet<int>(
                    IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Select(x => x.Port));
                foreach (var port in _settings.LocalPorts.Distinct())
                {
                    if (!listening.Contains(port)) continue;
                    evidence.Add(Fact("TCP cục bộ", "ConfiguredPortListening", port.ToString()));
                    warnings.Add("Cài đặt kiểm tra: cổng TCP cục bộ " + port + " đang lắng nghe.");
                }
            }
            catch (NetworkInformationException)
            {
                evidence.Add(Fact("TCP cục bộ", "InspectionUnavailable", "Không thể đọc danh sách cổng", ConfidenceLevel.Low));
            }
        }

        private void InspectServices(ICollection<ScanEvidence> evidence, ICollection<string> warnings, CancellationToken token)
        {
            if (_settings.ServiceKeywords.Count == 0) return;
            try
            {
                var rows = new WindowsWmiQueryService().Query("SELECT Name, DisplayName FROM Win32_Service");
                foreach (var row in rows)
                {
                    token.ThrowIfCancellationRequested();
                    var name = GetString(row, "Name");
                    var displayName = GetString(row, "DisplayName");
                    var match = FindMatch(name + " " + displayName, _settings.ServiceKeywords);
                    if (match.Length == 0) continue;
                    evidence.Add(Fact("WMI", "ConfiguredServiceKeyword", match));
                    warnings.Add("Cài đặt kiểm tra: phát hiện dịch vụ khớp từ khóa '" + match + "'.");
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                evidence.Add(Fact("WMI", "ServiceInspectionUnavailable", ex.GetType().Name, ConfidenceLevel.Low));
            }
        }

        private void InspectProcesses(ICollection<ScanEvidence> evidence, ICollection<string> warnings, CancellationToken token)
        {
            if (_settings.ProcessKeywords.Count == 0) return;
            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    using (process)
                    {
                        token.ThrowIfCancellationRequested();
                        string name;
                        try { name = process.ProcessName; }
                        catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception) { continue; }
                        var match = FindMatch(name, _settings.ProcessKeywords);
                        if (match.Length == 0) continue;
                        evidence.Add(Fact("Tiến trình", "ConfiguredProcessKeyword", match));
                        warnings.Add("Cài đặt kiểm tra: phát hiện tiến trình khớp từ khóa '" + match + "'.");
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                evidence.Add(Fact("Tiến trình", "ProcessInspectionUnavailable", ex.GetType().Name, ConfidenceLevel.Low));
            }
        }

        private void InspectPaths(ICollection<ScanEvidence> evidence, ICollection<string> warnings, CancellationToken token)
        {
            if (_settings.FilePaths.Count == 0) return;
            foreach (var configuredPath in _settings.FilePaths)
            {
                token.ThrowIfCancellationRequested();
                var path = Environment.ExpandEnvironmentVariables(configuredPath ?? string.Empty);
                if (path.Length == 0 || (!File.Exists(path) && !Directory.Exists(path))) continue;
                evidence.Add(Fact("Hệ thống tệp", "ConfiguredPathIndicator", path, ConfidenceLevel.Low, true));
                warnings.Add("Cài đặt kiểm tra: phát hiện đường dẫn đã cấu hình '" + path + "'.");
            }
        }

        private async Task InspectScheduledTasksAsync(
            SystemContext context,
            ICollection<ScanEvidence> evidence,
            ICollection<string> warnings,
            CancellationToken token)
        {
            if (_settings.TaskKeywords.Count == 0) return;
            var executable = Path.Combine(context.WindowsDirectory, "System32", "schtasks.exe");
            var result = await _processRunner.RunAsync(new ProcessExecutionRequest
            {
                ExecutablePath = executable,
                Arguments = "/query /fo csv /nh",
                Timeout = TimeSpan.FromSeconds(10)
            }, token).ConfigureAwait(false);
            if (result.ExitCode != 0 || result.StartFailure || result.TimedOut || result.WasCancelled)
            {
                evidence.Add(Fact("Task Scheduler", "TaskInspectionUnavailable", "Không thể đọc tác vụ", ConfidenceLevel.Low));
                return;
            }
            foreach (var keyword in _settings.TaskKeywords.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (result.StandardOutput.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0) continue;
                evidence.Add(Fact("Task Scheduler", "ConfiguredTaskKeyword", keyword));
                warnings.Add("Cài đặt kiểm tra: phát hiện tác vụ khớp từ khóa '" + keyword + "'.");
            }
        }

        private static ScanEvidence Fact(
            string source,
            string name,
            string value,
            ConfidenceLevel confidence = ConfidenceLevel.Medium,
            bool sensitive = false)
        {
            return new ScanEvidence
            {
                Source = source,
                Name = name,
                Value = value ?? string.Empty,
                Confidence = confidence,
                Sensitive = sensitive
            };
        }

        private static string FindMatch(string value, IEnumerable<string> keywords)
        {
            return keywords.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x) &&
                (value ?? string.Empty).IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0) ?? string.Empty;
        }

        private static string GetString(IReadOnlyDictionary<string, object?> row, string key)
        {
            object? value;
            return row.TryGetValue(key, out value) && value != null ? Convert.ToString(value) ?? string.Empty : string.Empty;
        }
    }
}
