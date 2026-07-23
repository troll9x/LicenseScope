using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LicenseScope.Windows.Classification;
using LicenseScope.Windows.Models;

namespace LicenseScope.App
{
    internal sealed class AuditSettings
    {
        internal static readonly int[] DefaultPorts = { 1688 };
        internal static readonly string[] DefaultServices =
        {
            "KMSpico", "KMService", "WinKSO", "KMSELDI", "KMS_VL_ALL",
            "KMSAuto", "AutoKMS", "KMSSS", "KMSEmulator", "vlmcsd", "Activation-Renewal"
        };
        internal static readonly string[] DefaultProcesses =
        {
            "KMSpico", "KMSELDI", "AutoKMS", "KMSAuto", "KMSguard",
            "WinKSO", "KMService", "vlmcsd", "AAct", "KMS_VL_ALL", "gatherosstate", "clipup"
        };
        internal static readonly string[] DefaultTaskKeywords =
        {
            "AutoKMS", "KMSAuto", "KMS_VL_ALL", "KMSpico", "KMSSS",
            "KMSEmulator", "KMService", "WinKSO", "vlmcsd", "Activation-Renewal"
        };
        internal static readonly string[] DefaultKmsDomains =
        {
            "msguides", "kms.loli", "digiboy.ir", "0t.ng", "kms.chinancce",
            "kmscloud", "kms.cangshui", "kms.ddns.net", "e8.us.to",
            "kms.mrxinwang", "kms8.msguides", "kms9.msguides", "kms.xspace.in", "skms.netnr"
        };
        internal static readonly string[] DefaultFilePaths =
        {
            @"C:\ProgramData\Microsoft\Windows\ClipSVC\GenuineTicket\GenuineTicket.xml",
            @"C:\Program Files\Activation-Renewal\Activation_task.cmd",
            @"C:\Program Files\Activation-Renewal\Info.txt"
        };

        public bool EnableExtendedInspection { get; set; } = true;
        public Dictionary<string, string> ExtraGenericKeys { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public List<int> ExtraPorts { get; set; } = new List<int>();
        public List<string> ExtraServices { get; set; } = new List<string>();
        public List<string> ExtraTaskKeywords { get; set; } = new List<string>();
        public List<string> ExtraProcesses { get; set; } = new List<string>();
        public List<string> ExtraFilePaths { get; set; } = new List<string>();
        public List<string> ExtraKmsDomains { get; set; } = new List<string>();

        public static string SettingsFilePath =>
            Path.Combine(ApplicationDataPaths.SettingsDirectory, "audit-settings.ini");

        public static AuditSettings Load()
        {
            var settings = new AuditSettings();
            if (!File.Exists(SettingsFilePath)) return settings;
            try
            {
                var section = string.Empty;
                foreach (var rawLine in File.ReadAllLines(SettingsFilePath))
                {
                    var line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        section = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }
                    settings.ReadValue(section, line);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return new AuditSettings();
            }
            return settings;
        }

        public void Save()
        {
            ApplicationDataPaths.EnsureSettingsDirectory();
            using (var writer = new StreamWriter(SettingsFilePath, false))
            {
                writer.WriteLine("# License Scope - Cài đặt kiểm tra chỉ đọc");
                writer.WriteLine("[General]");
                writer.WriteLine("EnableExtendedInspection=" + EnableExtendedInspection.ToString().ToLowerInvariant());
                WriteSection(writer, "UserGenericKeys", ExtraGenericKeys.OrderBy(x => x.Key).Select(x => x.Key + " = " + x.Value));
                WriteSection(writer, "ExtraPorts", ExtraPorts.Distinct().OrderBy(x => x).Select(x => x.ToString()));
                WriteSection(writer, "ExtraServices", ExtraServices);
                WriteSection(writer, "ExtraTaskKeywords", ExtraTaskKeywords);
                WriteSection(writer, "ExtraProcesses", ExtraProcesses);
                WriteSection(writer, "ExtraFilePaths", ExtraFilePaths);
                WriteSection(writer, "ExtraKmsDomains", ExtraKmsDomains);
            }
        }

        public WindowsInspectionSettings? ToWindowsInspectionSettings()
        {
            if (!EnableExtendedInspection) return null;
            return new WindowsInspectionSettings
            {
                LocalPorts = Merge(DefaultPorts, ExtraPorts),
                ServiceKeywords = Merge(DefaultServices, ExtraServices),
                TaskKeywords = Merge(DefaultTaskKeywords, ExtraTaskKeywords),
                ProcessKeywords = Merge(DefaultProcesses, ExtraProcesses),
                FilePaths = Merge(DefaultFilePaths, ExtraFilePaths),
                KmsDomainKeywords = Merge(DefaultKmsDomains, ExtraKmsDomains)
            };
        }

        private void ReadValue(string section, string line)
        {
            if (section.Equals("General", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(new[] { '=' }, 2);
                bool enabled;
                if (parts.Length == 2 &&
                    parts[0].Trim().Equals("EnableExtendedInspection", StringComparison.OrdinalIgnoreCase) &&
                    bool.TryParse(parts[1].Trim(), out enabled))
                    EnableExtendedInspection = enabled;
                return;
            }
            if (section.Equals("UserGenericKeys", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(new[] { '=' }, 2);
                var suffix = ExtractSuffix(parts[0]);
                if (suffix.Length == 5)
                    ExtraGenericKeys[suffix] = parts.Length == 2 ? parts[1].Trim() : "Khóa bổ sung";
                return;
            }
            if (section.Equals("ExtraPorts", StringComparison.OrdinalIgnoreCase))
            {
                int port;
                if (int.TryParse(line, out port) && port > 0 && port <= 65535) ExtraPorts.Add(port);
                return;
            }
            if (section.Equals("ExtraServices", StringComparison.OrdinalIgnoreCase)) ExtraServices.Add(line);
            else if (section.Equals("ExtraTaskKeywords", StringComparison.OrdinalIgnoreCase)) ExtraTaskKeywords.Add(line);
            else if (section.Equals("ExtraProcesses", StringComparison.OrdinalIgnoreCase)) ExtraProcesses.Add(line);
            else if (section.Equals("ExtraFilePaths", StringComparison.OrdinalIgnoreCase)) ExtraFilePaths.Add(line);
            else if (section.Equals("ExtraKmsDomains", StringComparison.OrdinalIgnoreCase)) ExtraKmsDomains.Add(line);
        }

        internal static string ExtractSuffix(string value)
        {
            var compact = new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
            return compact.Length < 5 ? string.Empty : compact.Substring(compact.Length - 5).ToUpperInvariant();
        }

        private static IReadOnlyList<T> Merge<T>(IEnumerable<T> defaults, IEnumerable<T> extra)
        {
            return defaults.Concat(extra).Distinct().ToArray();
        }

        private static IReadOnlyList<string> Merge(IEnumerable<string> defaults, IEnumerable<string> extra)
        {
            return defaults.Concat(extra).Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static void WriteSection(TextWriter writer, string name, IEnumerable<string> values)
        {
            writer.WriteLine();
            writer.WriteLine("[" + name + "]");
            foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                writer.WriteLine(value.Trim());
        }
    }

    internal sealed class ConfigurableWindowsKnownKeyCatalog : IWindowsKnownKeyCatalog
    {
        private readonly WindowsKnownKeyCatalog _builtIn = new WindowsKnownKeyCatalog();
        private readonly IReadOnlyDictionary<string, string> _extra;

        public ConfigurableWindowsKnownKeyCatalog(IReadOnlyDictionary<string, string> extra)
        {
            _extra = extra ?? new Dictionary<string, string>();
        }

        public bool IsGenericInstallationKey(string partialKey) =>
            _builtIn.IsGenericInstallationKey(partialKey) ||
            _extra.ContainsKey(partialKey ?? string.Empty);

        public bool IsVolumeClientKey(string partialKey) =>
            _builtIn.IsVolumeClientKey(partialKey);

        public string GetDescription(string partialKey)
        {
            var builtIn = _builtIn.GetDescription(partialKey);
            string description;
            return builtIn.Length > 0
                ? builtIn
                : _extra.TryGetValue(partialKey ?? string.Empty, out description)
                    ? description
                    : string.Empty;
        }
    }
}
