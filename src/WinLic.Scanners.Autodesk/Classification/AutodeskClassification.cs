using System;
using WinLic.Core.Models;
using WinLic.Scanners.Autodesk.Models;

namespace WinLic.Scanners.Autodesk.Classification
{
    public static class AutodeskLicenseMethodClassifier
    {
        public static string Classify(int? code) { switch (code) { case 1: return "Network"; case 2: return "StandaloneLegacy"; case 3: return "StandaloneDeploymentLegacy"; case 4: return "UserLicensing"; default: return "Unknown"; } }
        public static LicenseStatus Status(int? code) => code == 1 || code == 4 ? LicenseStatus.NeedsOnlineVerification : LicenseStatus.Unknown;
    }
    public static class AutodeskProductMatcher
    {
        public static AutodeskInstallation? Match(AutodeskRegistration registration, System.Collections.Generic.IEnumerable<AutodeskInstallation> installations)
        {
            AutodeskInstallation? yearMatch = null; var year = Year(registration.SelectedProductVersion);
            foreach (var installation in installations)
            {
                if (!string.IsNullOrWhiteSpace(registration.SelectedProductCode) && installation.ProductName.IndexOf(registration.SelectedProductCode, StringComparison.OrdinalIgnoreCase) >= 0) return installation;
                if (!string.IsNullOrWhiteSpace(registration.FeatureId) && installation.ProductName.IndexOf(registration.FeatureId, StringComparison.OrdinalIgnoreCase) >= 0) return installation;
                if (!string.IsNullOrWhiteSpace(year) && (installation.Version.StartsWith(year, StringComparison.OrdinalIgnoreCase) || installation.ProductName.IndexOf(year, StringComparison.OrdinalIgnoreCase) >= 0)) yearMatch = yearMatch == null ? installation : null;
            }
            return yearMatch;
        }
        private static string Year(string version) => !string.IsNullOrWhiteSpace(version) && version.Length >= 4 && int.TryParse(version.Substring(0, 4), out _) ? version.Substring(0, 4) : "";
    }
}
