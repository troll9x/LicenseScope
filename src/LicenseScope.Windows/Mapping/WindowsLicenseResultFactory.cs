using System;
using System.Collections.Generic;
using LicenseScope.Core.Models;
using LicenseScope.Core.Services;
using LicenseScope.Windows.Models;

namespace LicenseScope.Windows.Mapping
{
    public sealed class WindowsLicenseResultFactory
    {
        public LicenseResult Create(SystemContext context, WindowsLicenseProductRecord? product, WindowsLicenseEvidence evidence, WindowsActivationClassification classification)
        {
            var facts = new List<ScanEvidence>();
            if (product != null)
            {
                facts.Add(Fact("WMI", "LicenseStatus", product.LicenseStatus.HasValue ? product.LicenseStatus.Value.ToString() : "Unknown", ConfidenceLevel.High));
                if (product.Description.Length > 0) facts.Add(Fact("WMI", "Description", product.Description, ConfidenceLevel.Medium));
                if (product.PartialProductKey.Length > 0) facts.Add(Fact("WMI", "PartialProductKey", MaskPartial(product.PartialProductKey), ConfidenceLevel.Medium));
                if (product.GracePeriodRemaining.HasValue) facts.Add(Fact("WMI", "GracePeriodMinutes", product.GracePeriodRemaining.Value.ToString(), ConfidenceLevel.High));
                if (product.KmsMachineName.Length > 0) facts.Add(Fact("WMI", "KmsConfigured", "Yes", ConfidenceLevel.Medium));
            }
            if (evidence.Xpr.Parsed) facts.Add(Fact("slmgr /xpr", "Activation", evidence.Xpr.IsPermanent ? "Permanent" : evidence.Xpr.IndicatesUnlicensed ? "UnlicensedOrNotification" : "ExpirationParsed", ConfidenceLevel.Medium));
            if (evidence.MaskedOa3Key.Length > 0) facts.Add(Fact("WMI", "OA3FirmwareKey", evidence.MaskedOa3Key, ConfidenceLevel.Low, true));
            if (evidence.MaskedBackupKey.Length > 0) facts.Add(Fact("Registry", "BackupProductKey", evidence.MaskedBackupKey, ConfidenceLevel.Low, true));
            facts.Add(Fact("WindowsClassifier", "ActivationMethod", classification.ActivationMethod, classification.Confidence));

            var result = new LicenseResult
            {
                ScannerId = "microsoft.windows",
                Vendor = "Microsoft",
                ProductName = product?.Name.Length > 0 ? product.Name : context.OsName.Length > 0 ? context.OsName : "Windows",
                ProductVersion = context.OsVersion,
                Installed = true,
                Status = classification.Status,
                IsLicensed = LicenseStatusMapper.ToIsLicensed(classification.Status),
                LicenseType = classification.LicenseType,
                Confidence = classification.Confidence,
                PartialProductKey = MaskPartial(product?.PartialProductKey ?? evidence.Dlv.PartialProductKey),
                FullProductKey = SelectFullProductKey(product?.PartialProductKey ?? evidence.Dlv.PartialProductKey, evidence),
                ExpirationDate = classification.ExpirationDate,
                Evidence = facts,
                Warnings = classification.Warnings
            };
            var issues = LicenseResultValidator.NormalizeAndValidate(result);
            if (issues.Count > 0) throw new InvalidOperationException("Windows result invariant violation: " + string.Join("; ", issues));
            return result;
        }

        private static ScanEvidence Fact(string source, string name, string value, ConfidenceLevel confidence, bool sensitive = false) => new ScanEvidence { Source = source, Name = name, Value = value ?? string.Empty, Confidence = confidence, Sensitive = sensitive };
        private static string MaskPartial(string value)
        {
            var compact = (value ?? string.Empty).Replace("-", string.Empty).Trim();
            if (compact.Length == 0) return string.Empty;
            var suffix = compact.Length <= 5 ? compact : compact.Substring(compact.Length - 5);
            return "XXXXX-XXXXX-XXXXX-XXXXX-" + suffix.ToUpperInvariant();
        }

        private static string SelectFullProductKey(string partialProductKey, WindowsLicenseEvidence evidence)
        {
            var suffix = LastFive(partialProductKey);
            var candidates = new[] { evidence.BackupProductKey, evidence.Oa3ProductKey };
            foreach (var candidate in candidates)
            {
                if (IsFullProductKey(candidate) && suffix.Length > 0 &&
                    candidate.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return candidate.ToUpperInvariant();
            }
            if (suffix.Length > 0) return string.Empty;
            foreach (var candidate in candidates)
            {
                if (IsFullProductKey(candidate)) return candidate.ToUpperInvariant();
            }
            return string.Empty;
        }

        private static bool IsFullProductKey(string value)
        {
            var compact = (value ?? string.Empty).Replace("-", string.Empty).Trim();
            return compact.Length == 25;
        }

        private static string LastFive(string value)
        {
            var compact = (value ?? string.Empty).Replace("-", string.Empty).Trim();
            return compact.Length <= 5 ? compact.ToUpperInvariant() : compact.Substring(compact.Length - 5).ToUpperInvariant();
        }
    }
}
