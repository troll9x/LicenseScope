using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using LicenseScope.Office.Models;

namespace LicenseScope.Office.Detection
{
    public interface IOfficeInstallationDetector { IReadOnlyList<OfficeInstallation> Detect(); }

    public sealed class OfficeInstallationDetector : IOfficeInstallationDetector
    {
        public IReadOnlyList<OfficeInstallation> Detect()
        {
            var found = new List<OfficeInstallation>();
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (var key = hive.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration", false))
                    {
                        if (key == null) continue;
                        var ids = Convert.ToString(key.GetValue("ProductReleaseIds")) ?? string.Empty;
                        var root = Convert.ToString(key.GetValue("InstallationPath")) ?? string.Empty;
                        var version = Convert.ToString(key.GetValue("VersionToReport")) ?? string.Empty;
                        var arch = Convert.ToString(key.GetValue("Platform")) ?? (view == RegistryView.Registry32 ? "x86" : "x64");
                        foreach (var id in ids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                            Add(found, FromId(id.Trim(), version, arch, "ClickToRun", root, true));
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException || ex is IOException) { continue; }
            }

            foreach (var pair in CandidateRoots())
            {
                if (!Directory.Exists(pair.Item1)) continue;
                if (File.Exists(Path.Combine(pair.Item1, "WINWORD.EXE"))) Add(found, FromId("OfficeSuite", FileVersion(pair.Item1, "WINWORD.EXE"), pair.Item2, "MSIOrLocal", pair.Item1, false));
                if (File.Exists(Path.Combine(pair.Item1, "WINPROJ.EXE"))) Add(found, FromId("Project", FileVersion(pair.Item1, "WINPROJ.EXE"), pair.Item2, "MSIOrLocal", pair.Item1, false));
                if (File.Exists(Path.Combine(pair.Item1, "VISIO.EXE"))) Add(found, FromId("Visio", FileVersion(pair.Item1, "VISIO.EXE"), pair.Item2, "MSIOrLocal", pair.Item1, false));
            }
            return found;
        }

        private static IEnumerable<Tuple<string, string>> CandidateRoots()
        {
            foreach (var item in new[] { Tuple.Create(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "x64"), Tuple.Create(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "x86") })
                foreach (var suffix in new[] { @"Microsoft Office\Office16", @"Microsoft Office\root\Office16", @"Microsoft Office\Office15", @"Microsoft Office\Office14" })
                    if (!string.IsNullOrWhiteSpace(item.Item1)) yield return Tuple.Create(Path.Combine(item.Item1, suffix), item.Item2);
        }

        internal static OfficeInstallation FromId(string id, string version, string arch, string type, string root, bool vnext)
        {
            var lower = id.ToLowerInvariant();
            var family = lower.Contains("project") ? OfficeProductFamily.Project : lower.Contains("visio") ? OfficeProductFamily.Visio : lower.Contains("access") ? OfficeProductFamily.Access : lower.Contains("o365") || lower.Contains("m365") ? OfficeProductFamily.Microsoft365Apps : OfficeProductFamily.OfficeSuite;
            var name = family == OfficeProductFamily.Project ? "Microsoft Project" : family == OfficeProductFamily.Visio ? "Microsoft Visio" : family == OfficeProductFamily.Access ? "Microsoft Access" : family == OfficeProductFamily.Microsoft365Apps ? "Microsoft 365 Apps" : "Microsoft Office";
            var subscriptionId = family == OfficeProductFamily.Microsoft365Apps || ((family == OfficeProductFamily.Project || family == OfficeProductFamily.Visio) && (lower.Contains("retail") || lower.Contains("subscription")) && !lower.Contains("volume") && !lower.Contains("vl_"));
            return new OfficeInstallation { ProductId = id, ProductName = name, Family = family, Version = version, Architecture = arch, InstallationType = type, RootPath = root, UsesVNext = vnext && subscriptionId };
        }

        private static void Add(List<OfficeInstallation> list, OfficeInstallation value)
        {
            if (value.ProductId.IndexOf("language", StringComparison.OrdinalIgnoreCase) >= 0 || value.ProductId.IndexOf("proof", StringComparison.OrdinalIgnoreCase) >= 0 || value.ProductId.IndexOf("teams", StringComparison.OrdinalIgnoreCase) >= 0 || value.ProductId.IndexOf("onedrive", StringComparison.OrdinalIgnoreCase) >= 0) return;
            if (!list.Any(x => string.Equals(x.ProductId, value.ProductId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Architecture, value.Architecture, StringComparison.OrdinalIgnoreCase))) list.Add(value);
        }

        private static string FileVersion(string root, string file) { try { return System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.Combine(root, file)).FileVersion ?? string.Empty; } catch { return string.Empty; } }
    }

    public sealed class OfficeToolLocator
    {
        public IReadOnlyList<OfficeToolLocation> Locate(IEnumerable<OfficeInstallation> installations)
        {
            var candidates = new List<OfficeToolLocation>();
            foreach (var install in installations)
            {
                foreach (var root in new[] { install.RootPath, Path.Combine(install.RootPath, "Office16"), Path.Combine(install.RootPath, @"root\Office16") })
                {
                    AddIfFile(candidates, "OSPP", Path.Combine(root, "OSPP.VBS"), install.Architecture, "InstallationRoot");
                    AddIfFile(candidates, "VNext", Path.Combine(root, "vnextdiag.ps1"), install.Architecture, "InstallationRoot");
                }
            }
            return candidates.OrderBy(x => x.ToolType, StringComparer.Ordinal).ThenBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase).GroupBy(x => x.ToolType + "|" + x.FullPath, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToArray();
        }
        private static void AddIfFile(List<OfficeToolLocation> list, string type, string path, string arch, string source) { if (File.Exists(path)) list.Add(new OfficeToolLocation { ToolType = type, FullPath = Path.GetFullPath(path), Architecture = arch, Source = source }); }
    }
}
