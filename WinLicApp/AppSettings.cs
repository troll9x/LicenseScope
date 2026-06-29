// =============================================================================
// AppSettings.cs  --  WinLic Manager
// =============================================================================
// Persists user customizations for the Option 7 audit scan to settings.ini
// placed alongside the executable.  Built-in defaults are never written to
// disk as active entries -- the file documents them as comments.
//
// settings.ini section names use CamelCase (e.g. [ExtraPorts]) which is the
// canonical format shared with the PowerShell CLI.  The parser normalizes
// section headers by stripping underscores and uppercasing so both
// [ExtraPorts] and [EXTRA_PORTS] map to the same bucket.
// =============================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WinLicApp
{
    public static class AppSettings
    {
        // ── Resolved path ─────────────────────────────────────────────────────────
        private static readonly string SettingsPath =
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".",
                "settings.ini");

        // ── Built-in defaults (read-only, always active) ──────────────────────────
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

        // Known cloud KMS piracy services — minimal hardcoded safety net.
        // The FULL authoritative list lives in [KmsPiracyDomains] in settings.ini.
        // Matching is case-insensitive substring: "msguides" catches km8.msguides.com etc.
        public static readonly string[] DefaultKmsPiracyDomains =
        {
            "msguides",       // km8.msguides.com, kms2.msguides.com, kms9.msguides.com
            "kms.loli",       // kms.loli.beer
            "digiboy.ir",
            "0t.ng",
            "kms.chinancce",
            "kmscloud",
        };

        // ── User-added extras (populated by Load()) ───────────────────────────────
        public static List<int>    ExtraPorts            { get; set; } = new List<int>();
        public static List<string> ExtraServices         { get; set; } = new List<string>();
        public static List<string> ExtraProcesses        { get; set; } = new List<string>();
        public static List<string> ExtraTaskKeywords     { get; set; } = new List<string>();
        public static List<string> ExtraFilePaths        { get; set; } = new List<string>();
        public static List<string> ExtraKmsPiracyDomains { get; set; } = new List<string>();

        // ── Merged views (defaults + user additions) ──────────────────────────────
        public static int[]           AllPorts            => DefaultPorts.Concat(ExtraPorts).Distinct().ToArray();
        public static HashSet<string> AllServices         => new HashSet<string>(DefaultServices.Concat(ExtraServices),     StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> AllProcesses        => new HashSet<string>(DefaultProcesses.Concat(ExtraProcesses),   StringComparer.OrdinalIgnoreCase);
        public static string[]        AllTaskKeywords     => DefaultTaskKeywords.Concat(ExtraTaskKeywords).Distinct().ToArray();

        // Full piracy-domain list: built-in defaults + anything added in settings.ini.
        // Callers do a case-insensitive substring match against this list.
        public static string[]        AllKmsPiracyDomains => DefaultKmsPiracyDomains.Concat(ExtraKmsPiracyDomains)
                                                             .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        // ── IO ────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Normalizes a settings.ini section name so that [ExtraPorts],
        /// [EXTRA_PORTS], and [extra_ports] all resolve to "EXTRAPORTS".
        /// </summary>
        private static string NormalizeSection(string raw) =>
            raw.Replace("_", "").Replace(" ", "").ToUpperInvariant();

        public static void Load()
        {
            ExtraPorts.Clear(); ExtraServices.Clear();
            ExtraProcesses.Clear(); ExtraTaskKeywords.Clear();
            ExtraFilePaths.Clear(); ExtraKmsPiracyDomains.Clear();

            if (!File.Exists(SettingsPath)) return;
            try
            {
                string section = "";
                foreach (var raw in File.ReadAllLines(SettingsPath))
                {
                    var l = raw.Trim();
                    // Skip blanks and comments (both ; and # are comment chars)
                    if (string.IsNullOrEmpty(l) || l.StartsWith(";") || l.StartsWith("#")) continue;
                    if (l.StartsWith("[") && l.EndsWith("]"))
                    {
                        section = NormalizeSection(l.Substring(1, l.Length - 2));
                        continue;
                    }

                    switch (section)
                    {
                        // Both [ExtraPorts] and [EXTRA_PORTS] normalize to EXTRAPORTS
                        case "EXTRAPORTS":
                            if (int.TryParse(l, out int p)) ExtraPorts.Add(p); break;
                        case "EXTRASERVICES":
                            if (!string.IsNullOrWhiteSpace(l)) ExtraServices.Add(l); break;
                        case "EXTRAPROCESSES":
                            if (!string.IsNullOrWhiteSpace(l)) ExtraProcesses.Add(l); break;
                        // [ExtraTaskKeywords] and [EXTRA_TASK_KEYWORDS] both -> EXTRATASKEYWORDS
                        case "EXTRATASKEYWORDS":
                            if (!string.IsNullOrWhiteSpace(l)) ExtraTaskKeywords.Add(l); break;
                        case "EXTRAFILEPATHS":
                            if (!string.IsNullOrWhiteSpace(l)) ExtraFilePaths.Add(l); break;
                        case "KMSPIRACYDOMAINS":
                            if (!string.IsNullOrWhiteSpace(l)) ExtraKmsPiracyDomains.Add(l); break;
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
                w.WriteLine("; =============================================================================");
                w.WriteLine("; settings.ini  --  WinLic Manager -- Option 7 Scan Configuration");
                w.WriteLine("; =============================================================================");
                w.WriteLine("; User-added entries are merged with built-in defaults at scan time.");
                w.WriteLine("; Built-in defaults are shown as comments for reference only.");
                w.WriteLine("; Format: one value per line.  Lines starting with ; or # are ignored.");
                w.WriteLine("; Section names are case-insensitive (ExtraPorts = EXTRA_PORTS = extra_ports).");
                w.WriteLine("; =============================================================================");
                w.WriteLine();

                w.WriteLine("[ExtraPorts]");
                w.WriteLine("; Built-in: 1688");
                foreach (var p in ExtraPorts) w.WriteLine(p);
                w.WriteLine();

                w.WriteLine("[ExtraServices]");
                w.WriteLine("; Built-in: KMSpico, KMService, WinKSO, KMSELDI, KMS_VL_ALL, KMSAuto, AutoKMS, KMSSS, KMSEmulator, vlmcsd");
                foreach (var s in ExtraServices) w.WriteLine(s);
                w.WriteLine();

                w.WriteLine("[ExtraProcesses]");
                w.WriteLine("; Built-in: KMSpico, KMSELDI, AutoKMS, KMSAuto, KMSguard, WinKSO, KMService, vlmcsd, AAct, KMS_VL_ALL");
                foreach (var s in ExtraProcesses) w.WriteLine(s);
                w.WriteLine();

                w.WriteLine("[ExtraTaskKeywords]");
                w.WriteLine("; Built-in: AutoKMS, KMSAuto, KMS_VL_ALL, KMSpico, KMSSS, KMSEmulator, KMService, WinKSO, vlmcsd");
                foreach (var s in ExtraTaskKeywords) w.WriteLine(s);
                w.WriteLine();

                w.WriteLine("[ExtraFilePaths]");
                w.WriteLine("; Built-in: %ProgramFiles%\\KMSpico, %SystemRoot%\\System32\\KMSELDI.exe, %ProgramFiles%\\AAct (and more)");
                foreach (var s in ExtraFilePaths) w.WriteLine(s);
                w.WriteLine();

                w.WriteLine("[KmsPiracyDomains]");
                w.WriteLine("; Built-in piracy domains are always active (listed as comments for reference).");
                w.WriteLine("; To add a new domain, add it as an uncommented line below.");
                w.WriteLine("; To request a domain be added to the built-in list, open a GitHub issue:");
                w.WriteLine(";   https://github.com/ardennguyen/WinLic/issues");
                w.WriteLine("; Built-in: msguides, kms.loli, digiboy.ir, 0t.ng, kms.chinancce, kmscloud");
                foreach (var s in ExtraKmsPiracyDomains) w.WriteLine(s);
            }
            catch { /* ignore write failures (e.g. read-only location) */ }
        }

        public static string SettingsFilePath => SettingsPath;
    }
}
