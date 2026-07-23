using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using LicenseScope.Core.Models;

namespace LicenseScope.App
{
    internal sealed class RegisteredProgram
    {
        public string DisplayName { get; set; } = string.Empty;
        public string DisplayVersion { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string RegistryKeyName { get; set; } = string.Empty;
        public string UninstallString { get; set; } = string.Empty;
        public bool IsWindowsInstaller { get; set; }
    }

    internal sealed class SoftwareUninstallResult
    {
        public bool Started { get; set; }
        public bool WasCancelled { get; set; }
        public bool RequiresManualRemoval { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    internal sealed class SoftwareUninstallService
    {
        private static readonly Regex ProductCode = new Regex(
            @"\{[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\}",
            RegexOptions.Compiled);

        private static readonly HashSet<string> BlockedExecutables =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "cmd.exe", "powershell.exe", "pwsh.exe", "wscript.exe", "cscript.exe",
                "rundll32.exe", "regsvr32.exe", "mshta.exe"
            };

        public RegisteredProgram? FindRegisteredProgram(LicenseResult product)
        {
            if (product == null || !product.Installed ||
                product.ScannerId.Equals("microsoft.windows", StringComparison.OrdinalIgnoreCase))
                return null;

            return ReadRegisteredPrograms()
                .Select(program => new { Program = program, Score = MatchScore(product, program) })
                .Where(match => match.Score >= 120)
                .OrderByDescending(match => match.Score)
                .ThenBy(match => match.Program.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(match => match.Program)
                .FirstOrDefault();
        }

        public Task<SoftwareUninstallResult> StartUninstallAsync(RegisteredProgram program)
        {
            if (program == null) throw new ArgumentNullException(nameof(program));
            return Task.Run(() =>
            {
                var result = new SoftwareUninstallResult();
                try
                {
                    ProcessStartInfo? startInfo;
                    string productCode;
                    if (TryGetMsiProductCode(program, out productCode))
                    {
                        startInfo = new ProcessStartInfo
                        {
                            FileName = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.System),
                                "msiexec.exe"),
                            Arguments = "/x " + productCode,
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                    }
                    else
                    {
                        startInfo = CreateRegisteredUninstaller(program.UninstallString);
                        if (startInfo == null)
                        {
                            result.RequiresManualRemoval = true;
                            result.ErrorMessage =
                                "Trình gỡ cài đặt đã đăng ký không thuộc định dạng an toàn được hỗ trợ.";
                            return result;
                        }
                    }

                    var process = Process.Start(startInfo);
                    if (process == null)
                    {
                        result.ErrorMessage = "Windows không thể khởi chạy trình gỡ cài đặt.";
                        return result;
                    }
                    process.Dispose();
                    result.Started = true;
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    result.WasCancelled = true;
                    result.ErrorMessage = "Đã hủy yêu cầu quyền quản trị.";
                }
                catch (Exception ex) when (
                    ex is Win32Exception ||
                    ex is InvalidOperationException ||
                    ex is UnauthorizedAccessException ||
                    ex is IOException)
                {
                    result.ErrorMessage = ex.Message;
                }
                return result;
            });
        }

        public bool OpenInstalledApps()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:appsfeatures",
                    UseShellExecute = true
                })?.Dispose();
                return true;
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                return false;
            }
        }

        private static IReadOnlyList<RegisteredProgram> ReadRegisteredPrograms()
        {
            var results = new List<RegisteredProgram>();
            var hives = new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser };
            var views = Environment.Is64BitOperatingSystem
                ? new[] { RegistryView.Registry64, RegistryView.Registry32 }
                : new[] { RegistryView.Registry32 };

            foreach (var hiveName in hives)
            foreach (var view in views)
            {
                try
                {
                    using (var hive = RegistryKey.OpenBaseKey(hiveName, view))
                    using (var root = hive.OpenSubKey(
                               @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", false))
                    {
                        if (root == null) continue;
                        foreach (var keyName in root.GetSubKeyNames())
                        {
                            using (var key = root.OpenSubKey(keyName, false))
                            {
                                if (key == null) continue;
                                var displayName = Convert.ToString(key.GetValue("DisplayName")) ?? string.Empty;
                                var uninstall = Convert.ToString(key.GetValue("UninstallString")) ?? string.Empty;
                                if (string.IsNullOrWhiteSpace(displayName) ||
                                    string.IsNullOrWhiteSpace(uninstall) ||
                                    ReadInteger(key.GetValue("SystemComponent", 0)) == 1)
                                    continue;
                                results.Add(new RegisteredProgram
                                {
                                    DisplayName = displayName.Trim(),
                                    DisplayVersion =
                                        (Convert.ToString(key.GetValue("DisplayVersion")) ?? string.Empty).Trim(),
                                    Publisher =
                                        (Convert.ToString(key.GetValue("Publisher")) ?? string.Empty).Trim(),
                                    RegistryKeyName = keyName,
                                    UninstallString = uninstall.Trim(),
                                    IsWindowsInstaller =
                                        ReadInteger(key.GetValue("WindowsInstaller", 0)) == 1
                                });
                            }
                        }
                    }
                }
                catch (Exception ex) when (
                    ex is UnauthorizedAccessException ||
                    ex is System.Security.SecurityException ||
                    ex is IOException)
                {
                    // A missing or inaccessible registry view must not block other views.
                }
            }

            return results
                .GroupBy(x => x.DisplayName + "|" + x.DisplayVersion, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToArray();
        }

        private static int MatchScore(LicenseResult product, RegisteredProgram program)
        {
            var expected = Normalize(product.ProductName);
            var actual = Normalize(program.DisplayName);
            if (expected.Length < 4 || actual.Length < 4) return 0;

            var score = expected.Equals(actual, StringComparison.OrdinalIgnoreCase)
                ? 220
                : expected.Contains(actual) || actual.Contains(expected) ? 140 : 0;

            if (score == 0)
            {
                var expectedTokens = Tokens(expected);
                var actualTokens = Tokens(actual);
                var common = expectedTokens.Intersect(actualTokens, StringComparer.OrdinalIgnoreCase).Count();
                if (common >= 3) score = 100 + common * 5;
            }

            if (!string.IsNullOrWhiteSpace(product.ProductVersion) &&
                program.DisplayVersion.StartsWith(product.ProductVersion, StringComparison.OrdinalIgnoreCase))
                score += 15;
            if (!string.IsNullOrWhiteSpace(product.Vendor) &&
                program.Publisher.IndexOf(product.Vendor, StringComparison.OrdinalIgnoreCase) >= 0)
                score += 10;
            return score;
        }

        private static int ReadInteger(object? value)
        {
            int parsed;
            return int.TryParse(Convert.ToString(value), out parsed) ? parsed : 0;
        }

        private static string Normalize(string value)
        {
            return Regex.Replace((value ?? string.Empty).ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
        }

        private static IReadOnlyList<string> Tokens(string value)
        {
            return value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length > 2 &&
                            !x.Equals("microsoft", StringComparison.OrdinalIgnoreCase) &&
                            !x.Equals("desktop", StringComparison.OrdinalIgnoreCase) &&
                            !x.Equals("product", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool TryGetMsiProductCode(RegisteredProgram program, out string productCode)
        {
            var match = ProductCode.Match(program.RegistryKeyName);
            if (!match.Success) match = ProductCode.Match(program.UninstallString);
            productCode = match.Success ? match.Value : string.Empty;
            return match.Success && (program.IsWindowsInstaller ||
                                     program.UninstallString.IndexOf(
                                         "msiexec", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static ProcessStartInfo? CreateRegisteredUninstaller(string command)
        {
            string executable;
            string arguments;
            var trimmed = Environment.ExpandEnvironmentVariables(command ?? string.Empty).Trim();
            if (trimmed.StartsWith("\"", StringComparison.Ordinal))
            {
                var closingQuote = trimmed.IndexOf('"', 1);
                if (closingQuote <= 1) return null;
                executable = trimmed.Substring(1, closingQuote - 1);
                arguments = trimmed.Substring(closingQuote + 1).Trim();
            }
            else
            {
                var executableEnd = trimmed.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                if (executableEnd < 0) return null;
                executableEnd += 4;
                executable = trimmed.Substring(0, executableEnd).Trim();
                arguments = trimmed.Substring(executableEnd).Trim();
            }

            if (!Path.IsPathRooted(executable) || !File.Exists(executable) ||
                BlockedExecutables.Contains(Path.GetFileName(executable)))
                return null;

            return new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(executable) ?? string.Empty,
                UseShellExecute = true,
                Verb = "runas"
            };
        }
    }
}
