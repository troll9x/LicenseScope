using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using LicenseScope.Windows.Acquisition;

namespace LicenseScope.App
{
    internal sealed class KmsSettingsSnapshot
    {
        public IReadOnlyList<string> Hosts { get; set; } = Array.Empty<string>();
        public string RegistryHost { get; set; } = string.Empty;
        public int? RegistryPort { get; set; }
        public bool IsConfigured => Hosts.Count > 0 || RegistryHost.Length > 0;

        public string Describe()
        {
            var host = Hosts.FirstOrDefault() ?? RegistryHost;
            if (host.Length == 0) return "Không phát hiện máy chủ KMS được cấu hình thủ công.";
            return "Máy chủ KMS: " + host + Environment.NewLine +
                   "Cổng: " + (RegistryPort.HasValue ? RegistryPort.Value.ToString() : "mặc định");
        }
    }

    internal sealed class KmsClearResult
    {
        public bool Success { get; set; }
        public bool WasCancelled { get; set; }
        public int? ExitCode { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    internal sealed class KmsManagementService
    {
        private const string WindowsApplicationId = "55c92734-d682-4d71-983e-d6ec3f16059f";

        public KmsSettingsSnapshot Probe()
        {
            var hosts = new List<string>();
            try
            {
                var query = "SELECT KeyManagementServiceMachine FROM SoftwareLicensingProduct WHERE ApplicationID='" +
                            WindowsApplicationId + "'";
                foreach (var row in new WindowsWmiQueryService().Query(query))
                {
                    object? value;
                    var host = row.TryGetValue("KeyManagementServiceMachine", out value)
                        ? Convert.ToString(value) ?? string.Empty
                        : string.Empty;
                    if (!string.IsNullOrWhiteSpace(host)) hosts.Add(host.Trim());
                }
            }
            catch { }

            var registryHost = string.Empty;
            int? registryPort = null;
            foreach (var path in new[]
                     {
                         @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareLicensingService",
                         @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform"
                     })
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(path, false))
                    {
                        if (key == null) continue;
                        var host = Convert.ToString(key.GetValue("KeyManagementServiceMachine")) ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(host))
                            host = Convert.ToString(key.GetValue("KeyManagementServiceName")) ?? string.Empty;
                        if (registryHost.Length == 0 && !string.IsNullOrWhiteSpace(host))
                            registryHost = host.Trim();
                        int port;
                        if (!registryPort.HasValue &&
                            int.TryParse(Convert.ToString(key.GetValue("KeyManagementServicePort")), out port))
                            registryPort = port;
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException) { }
            }

            return new KmsSettingsSnapshot
            {
                Hosts = hosts.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                RegistryHost = registryHost,
                RegistryPort = registryPort
            };
        }

        public Task<KmsClearResult> ClearWindowsKmsSettingsAsync()
        {
            return Task.Run(() =>
            {
                var result = new KmsClearResult();
                try
                {
                    var systemDirectory = ResolveNativeSystemDirectory();
                    var cscript = Path.Combine(systemDirectory, "cscript.exe");
                    var slmgr = Path.Combine(systemDirectory, "slmgr.vbs");
                    if (!File.Exists(cscript) || !File.Exists(slmgr))
                    {
                        result.ErrorMessage = "Không tìm thấy công cụ quản lý bản quyền Windows.";
                        return result;
                    }
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = cscript,
                        Arguments = "//Nologo \"" + slmgr + "\" /ckms",
                        WorkingDirectory = systemDirectory,
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using (var process = Process.Start(startInfo))
                    {
                        if (process == null)
                        {
                            result.ErrorMessage = "Không thể khởi chạy công cụ Windows.";
                            return result;
                        }
                        process.WaitForExit();
                        result.ExitCode = process.ExitCode;
                        result.Success = process.ExitCode == 0;
                        if (!result.Success)
                            result.ErrorMessage = "Windows trả về mã lỗi " + process.ExitCode + ".";
                    }
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    result.WasCancelled = true;
                    result.ErrorMessage = "Đã hủy yêu cầu quyền quản trị.";
                }
                catch (Exception ex) when (
                    ex is Win32Exception ||
                    ex is InvalidOperationException ||
                    ex is UnauthorizedAccessException)
                {
                    result.ErrorMessage = ex.Message;
                }
                return result;
            });
        }

        private static string ResolveNativeSystemDirectory()
        {
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            return Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                ? Path.Combine(windows, "Sysnative")
                : Path.Combine(windows, "System32");
        }
    }
}
