using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WinLicApp
{
    /// <summary>
    /// Persists user customisations for the Option 7 audit scan to settings.ini
    /// placed alongside the executable.  Built-in defaults are never written to
    /// disk — the file only stores user-added entries.
    /// </summary>
    public static class AppSettings
    {
        // ── Resolved path ─────────────────────────────────────────────────────────
        private static readonly string SettingsPath =
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".",
                "settings.ini");

        // ── Built-in defaults (read-only) ─────────────────────────────────────────
        public static readonly int[]    DefaultPorts        = { 1688 };
        public static readonly string[] DefaultServices     =
        {
            "KMSpico", "KMService", "WinKSO", "KMSELDI", "KMS_VL_ALL",
            "KMSAuto", "AutoKMS",  "KMSSS",  "KMSEmulator", "vlmcsd"
        };
        public static readonly string[] DefaultProcesses    =
        {
            "KMSpico", "KMSELDI", "AutoKMS", "KMSAuto", "KMSguard",
            "WinKSO",  "KMService", "vlmcsd", "AAct",   "KMS_VL_ALL"
        };
        public static readonly string[] DefaultTaskKeywords =
        {
            "AutoKMS", "KMSAuto", "KMS_VL_ALL", "KMSpico",
            "KMSSS",   "KMSEmulator", "KMService", "WinKSO", "vlmcsd"
        };

        // ── User-added extras (populated by Load / Settings dialog) ───────────────
        public static List<int>    ExtraPorts        { get; set; } = new List<int>();
        public static List<string> ExtraServices     { get; set; } = new List<string>();
        public static List<string> ExtraProcesses    { get; set; } = new List<string>();
        public static List<string> ExtraTaskKeywords { get; set; } = new List<string>();
        public static List<string> ExtraFilePaths    { get; set; } = new List<string>();

        // ── Merged views (defaults + user additions) ──────────────────────────────
        public static int[]               AllPorts        => DefaultPorts.Concat(ExtraPorts).Distinct().ToArray();
        public static HashSet<string>     AllServices     => new HashSet<string>(DefaultServices.Concat(ExtraServices),     StringComparer.OrdinalIgnoreCase);
        public static HashSet<string>     AllProcesses    => new HashSet<string>(DefaultProcesses.Concat(ExtraProcesses),   StringComparer.OrdinalIgnoreCase);
        public static string[]            AllTaskKeywords => DefaultTaskKeywords.Concat(ExtraTaskKeywords).Distinct().ToArray();

        // ── IO ────────────────────────────────────────────────────────────────────
        public static void Load()
        {
            ExtraPorts.Clear(); ExtraServices.Clear();
            ExtraProcesses.Clear(); ExtraTaskKeywords.Clear(); ExtraFilePaths.Clear();

            if (!File.Exists(SettingsPath)) return;
            try
            {
                string section = "";
                foreach (var raw in File.ReadAllLines(SettingsPath))
                {
                    var l = raw.Trim();
                    if (string.IsNullOrEmpty(l) || l.StartsWith(";")) continue;
                    if (l.StartsWith("[") && l.EndsWith("]"))
                    { section = l.Substring(1, l.Length - 2).ToUpperInvariant(); continue; }

                    switch (section)
                    {
                        case "EXTRA_PORTS":
                            if (int.TryParse(l, out int p)) ExtraPorts.Add(p); break;
                        case "EXTRA_SERVICES":
                            if (!string.IsNullOrWhiteSpace(l)) ExtraServices.Add(l); break;
                        case "EXTRA_PROCESSES":
                            if (!string.IsNullOrWhiteSpace(l)) ExtraProcesses.Add(l); break;
                        case "EXTRA_TASK_KEYWORDS":
                            if (!string.IsNullOrWhiteSpace(l)) ExtraTaskKeywords.Add(l); break;
                        case "EXTRA_FILE_PATHS":
                            if (!string.IsNullOrWhiteSpace(l)) ExtraFilePaths.Add(l); break;
                    }
                }
            }
            catch { /* silently ignore corrupt settings */ }
        }

        public static void Save()
        {
            try
            {
                using var w = new StreamWriter(SettingsPath, append: false);
                w.WriteLine("; WinLic Manager — settings.ini");
                w.WriteLine("; User-added entries for the Option 7 scan (one value per line).");
                w.WriteLine("; Defaults are built-in and are NOT listed here.");
                w.WriteLine();
                w.WriteLine("[EXTRA_PORTS]");
                foreach (var p in ExtraPorts) w.WriteLine(p);
                w.WriteLine();
                w.WriteLine("[EXTRA_SERVICES]");
                foreach (var s in ExtraServices) w.WriteLine(s);
                w.WriteLine();
                w.WriteLine("[EXTRA_PROCESSES]");
                foreach (var s in ExtraProcesses) w.WriteLine(s);
                w.WriteLine();
                w.WriteLine("[EXTRA_TASK_KEYWORDS]");
                foreach (var s in ExtraTaskKeywords) w.WriteLine(s);
                w.WriteLine();
                w.WriteLine("[EXTRA_FILE_PATHS]");
                foreach (var s in ExtraFilePaths) w.WriteLine(s);
            }
            catch { /* ignore write failures (e.g. read-only location) */ }
        }

        public static string SettingsFilePath => SettingsPath;
    }
}
