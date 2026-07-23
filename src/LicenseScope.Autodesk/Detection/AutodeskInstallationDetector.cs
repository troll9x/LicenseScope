using System;
using System.Collections.Generic;
using Microsoft.Win32;
using LicenseScope.Core.Models;
using LicenseScope.Autodesk.Contracts;
using LicenseScope.Autodesk.Models;

namespace LicenseScope.Autodesk.Detection
{
    public sealed class AutodeskInstallationDetector : IAutodeskInstallationDetector
    {
        private static readonly string[] Excluded = { "access", "desktop app", "licensing", "genuine", "material library", "language pack", "content pack", "identity manager", "single sign on", "save to web" };
        public IReadOnlyList<AutodeskInstallation> Detect(SystemContext context, out IReadOnlyList<string> warnings)
        {
            var found = new Dictionary<string, AutodeskInstallation>(StringComparer.OrdinalIgnoreCase); var notes = new List<string>();
            ReadView(RegistryView.Registry32, found, notes); if (Environment.Is64BitOperatingSystem) ReadView(RegistryView.Registry64, found, notes);
            warnings = notes; var result = new List<AutodeskInstallation>(found.Values); result.Sort((a,b) => string.Compare(a.ProductName + a.Version, b.ProductName + b.Version, StringComparison.OrdinalIgnoreCase)); return result;
        }
        internal static bool IsProduct(string publisher, string name)
        {
            if (publisher.IndexOf("Autodesk", StringComparison.OrdinalIgnoreCase) < 0 || string.IsNullOrWhiteSpace(name)) return false;
            foreach (var excluded in Excluded) if (name.IndexOf(excluded, StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return true;
        }
        private static void ReadView(RegistryView view, IDictionary<string, AutodeskInstallation> found, ICollection<string> warnings)
        {
            try
            {
                using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (var uninstall = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", false))
                {
                    if (uninstall == null) return;
                    foreach (var keyName in uninstall.GetSubKeyNames()) using (var key = uninstall.OpenSubKey(keyName, false))
                    {
                        var name = key?.GetValue("DisplayName") as string ?? ""; var publisher = key?.GetValue("Publisher") as string ?? "";
                        if (!IsProduct(publisher, name)) continue;
                        var version = key?.GetValue("DisplayVersion") as string ?? ""; var code = key?.GetValue("ProductCode") as string ?? keyName;
                        found[name + "|" + version] = new AutodeskInstallation { ProductName = name, Version = version, ProductCode = code, Source = "Registry uninstall metadata (" + view + ")" };
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException || ex is System.IO.IOException) { warnings.Add("Autodesk installation metadata unavailable: " + ex.GetType().Name); }
        }
    }
}
