using System;
using System.Collections.Generic;
using System.Linq;
using LicenseScope.Windows.Acquisition;
using LicenseScope.Windows.Models;

namespace LicenseScope.Windows.Classification
{
    public sealed class WindowsProductSelection
    {
        public WindowsLicenseProductRecord? Product { get; set; }
        public bool Ambiguous { get; set; }
        public int CandidateCount { get; set; }
    }

    public sealed class WindowsProductSelector
    {
        public WindowsProductSelection Select(IReadOnlyList<WindowsLicenseProductRecord> records)
        {
            var candidates = (records ?? Array.Empty<WindowsLicenseProductRecord>())
                .Where(IsWindowsRecord)
                .OrderByDescending(Score)
                .ThenBy(record => record.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(record => record.Description, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (candidates.Length == 0) return new WindowsProductSelection();
            var topScore = Score(candidates[0]);
            return new WindowsProductSelection
            {
                Product = candidates[0],
                CandidateCount = candidates.Length,
                Ambiguous = candidates.Length > 1 && Score(candidates[1]) == topScore
            };
        }

        private static bool IsWindowsRecord(WindowsLicenseProductRecord record)
        {
            if (record == null) return false;
            return string.Equals(record.ApplicationId, WindowsEvidenceCollector.WindowsApplicationId, StringComparison.OrdinalIgnoreCase) ||
                record.Name.StartsWith("Windows", StringComparison.OrdinalIgnoreCase) ||
                record.Name.StartsWith("Microsoft Windows", StringComparison.OrdinalIgnoreCase);
        }

        private static int Score(WindowsLicenseProductRecord record)
        {
            var score = 0;
            if (record.LicenseStatus == 1) score += 100;
            else if (record.LicenseStatus == 2 || record.LicenseStatus == 3 || record.LicenseStatus == 4 || record.LicenseStatus == 6) score += 50;
            if (record.PartialProductKey.Length == 5) score += 20;
            if (string.Equals(record.ApplicationId, WindowsEvidenceCollector.WindowsApplicationId, StringComparison.OrdinalIgnoreCase)) score += 10;
            if (record.Description.Length > 0) score += 2;
            return score;
        }
    }
}
