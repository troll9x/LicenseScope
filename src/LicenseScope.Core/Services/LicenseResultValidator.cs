using System;
using System.Collections.Generic;
using LicenseScope.Core.Models;

namespace LicenseScope.Core.Services
{
    /// <summary>Normalizes collections and validates invariants of a license result.</summary>
    public static class LicenseResultValidator
    {
        public static IReadOnlyList<string> NormalizeAndValidate(LicenseResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Evidence = result.Evidence ?? Array.Empty<ScanEvidence>();
            result.Warnings = result.Warnings ?? Array.Empty<string>();

            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(result.ScannerId)) issues.Add("ScannerId is required.");
            if (string.IsNullOrWhiteSpace(result.Vendor)) issues.Add("Vendor is required.");
            if (string.IsNullOrWhiteSpace(result.ProductName)) issues.Add("ProductName is required.");
            if (result.IsLicensed != LicenseStatusMapper.ToIsLicensed(result.Status))
                issues.Add("IsLicensed is inconsistent with Status.");
            if (!result.Installed && result.Status == LicenseStatus.Licensed)
                issues.Add("A product cannot be Licensed when Installed is false.");
            return issues;
        }
    }
}
