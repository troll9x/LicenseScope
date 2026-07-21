using System;
using System.Collections.Generic;
using WinLic.Core.Models;
using WinLic.Scanners.Office.Classification;
using WinLic.Scanners.Office.Models;

namespace WinLic.Scanners.Office.Mapping
{
    public sealed class OfficeLicenseResultFactory
    {
        private readonly OfficeLicenseClassifier _classifier;
        public OfficeLicenseResultFactory(OfficeLicenseClassifier classifier) { _classifier = classifier; }
        public LicenseResult Create(OfficeProductEvidence product, IReadOnlyList<string> sharedWarnings)
        {
            var status = _classifier.Status(product); var type = _classifier.LicenseType(product); var evidence = new List<ScanEvidence>(); var warnings = new List<string>(sharedWarnings ?? Array.Empty<string>());
            evidence.Add(new ScanEvidence { Source = product.FromOfficialTool ? "MicrosoftDiagnostic" : "OfficeInstallation", Name = "ProductId", Value = product.ProductId, Confidence = product.FromOfficialTool ? ConfidenceLevel.Medium : ConfidenceLevel.Low });
            if (product.Architecture.Length > 0) evidence.Add(new ScanEvidence { Source = "OfficeInstallation", Name = "Architecture", Value = product.Architecture, Confidence = ConfidenceLevel.Low });
            if (product.InstallationType.Length > 0) evidence.Add(new ScanEvidence { Source = "OfficeInstallation", Name = "InstallationType", Value = product.InstallationType, Confidence = ConfidenceLevel.Low });
            if (product.MaskedAccount.Length > 0) evidence.Add(new ScanEvidence { Source = "VNextDiagnostic", Name = "Account", Value = product.MaskedAccount, Confidence = ConfidenceLevel.Medium, Sensitive = true });
            if (type == "Volume_KMSCLIENT") warnings.Add("Periodic KMS activation renewal may be required; this is not a subscription expiration date.");
            if (status == LicenseStatus.NeedsOnlineVerification) warnings.Add("Online verification is required; installation alone is not proof of a license.");
            if (product.PartiallyParsed) warnings.Add("Localized diagnostic output was only partially parsed.");
            return new LicenseResult { ScannerId = "microsoft.office", Vendor = "Microsoft", ProductName = string.IsNullOrWhiteSpace(product.ProductName) ? "Microsoft Office product" : product.ProductName, ProductVersion = product.Version, Installed = true, Status = status, IsLicensed = status == LicenseStatus.Licensed ? true : status == LicenseStatus.Unlicensed || status == LicenseStatus.Expired ? false : (bool?)null, LicenseType = type, Confidence = product.FromOfficialTool ? ConfidenceLevel.Medium : ConfidenceLevel.Low, PartialProductKey = Mask(product.PartialProductKey), ExpirationDate = product.ExpirationDate, Evidence = evidence, Warnings = warnings };
        }
        private static string Mask(string value) { if (string.IsNullOrWhiteSpace(value)) return string.Empty; var compact = value.Replace("-", string.Empty); var last = compact.Length <= 5 ? compact : compact.Substring(compact.Length - 5); return "XXXXX-XXXXX-XXXXX-XXXXX-" + last.ToUpperInvariant(); }
    }
}
