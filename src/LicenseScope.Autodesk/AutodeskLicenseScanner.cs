using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;
using LicenseScope.Autodesk.Classification;
using LicenseScope.Autodesk.Contracts;
using LicenseScope.Autodesk.Models;

namespace LicenseScope.Autodesk
{
    public sealed class AutodeskLicenseScanner : ILicenseScanner
    {
        private readonly IAutodeskInstallationDetector _detector; private readonly IAutodeskLicensingEvidenceProvider _evidence; private readonly IAutodeskServiceStatusProvider _service;
        public AutodeskLicenseScanner(IAutodeskInstallationDetector detector, IAutodeskLicensingEvidenceProvider evidence, IAutodeskServiceStatusProvider service) { _detector = detector; _evidence = evidence; _service = service; }
        public string ScannerId => "autodesk.desktop"; public string VendorName => "Autodesk";
        public bool IsApplicable(SystemContext context) => context != null && (context.OsName.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0 || !string.IsNullOrWhiteSpace(context.WindowsDirectory));
        public async Task<IReadOnlyList<LicenseResult>> ScanAsync(SystemContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested(); var installations = _detector.Detect(context, out var detectionWarnings); var service = _service.GetStatus(); var evidence = await _evidence.ListAsync(cancellationToken).ConfigureAwait(false);
            var results = new List<LicenseResult>(); var matched = new HashSet<AutodeskInstallation>(); var commonWarnings = detectionWarnings.Concat(evidence.Warnings).Concat(string.IsNullOrWhiteSpace(service.Warning) ? Array.Empty<string>() : new[] { service.Warning }).ToArray();
            foreach (var registration in evidence.Registrations)
            {
                cancellationToken.ThrowIfCancellationRequested(); var install = AutodeskProductMatcher.Match(registration, installations); if (install != null) matched.Add(install); results.Add(Create(install, registration, service, commonWarnings));
            }
            foreach (var install in installations.Where(x => !matched.Contains(x))) results.Add(Create(install, null, service, commonWarnings));
            return results.OrderBy(x => x.ProductName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.ProductVersion, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        private LicenseResult Create(AutodeskInstallation? install, AutodeskRegistration? registration, AutodeskServiceStatus service, IReadOnlyList<string> inherited)
        {
            var warnings = new List<string>(inherited); var method = AutodeskLicenseMethodClassifier.Classify(registration?.LicenseMethodCode); var status = registration == null ? LicenseStatus.Unknown : AutodeskLicenseMethodClassifier.Status(registration.LicenseMethodCode);
            if (registration == null) warnings.Add("Installed product is not present in Autodesk Licensing Service registration metadata.");
            if (method == "UserLicensing") warnings.Add("Local registration cannot distinguish Named User subscription from Flex or verify entitlement.");
            if (method == "Network") warnings.Add("Configured network licensing does not prove a current license checkout; server addresses are suppressed.");
            if (service.Found && !service.Running) warnings.Add("Autodesk Desktop Licensing Service is not running; this is not proof of an unlicensed product.");
            var name = install?.ProductName ?? registration?.SelectedProductCode ?? registration?.DefaultProductCode ?? registration?.FeatureId ?? "Autodesk registered product";
            var version = install?.Version ?? registration?.SelectedProductVersion ?? registration?.DefaultProductVersion ?? "";
            var facts = new List<ScanEvidence> { new ScanEvidence { Source = "Autodesk licensing metadata", Name = "Registration", Value = registration == null ? "Not observed" : "Observed", Confidence = ConfidenceLevel.Medium }, new ScanEvidence { Source = "Windows Service Control Manager", Name = "Licensing service", Value = !service.Found ? "Not found" : service.Running ? "Running" : "Not running", Confidence = ConfidenceLevel.Medium } };
            if (registration != null) facts.Add(new ScanEvidence { Source = "AdskLicensingInstHelper list", Name = "License method", Value = method, Confidence = ConfidenceLevel.High });
            return new LicenseResult { ScannerId = ScannerId, Vendor = VendorName, ProductName = name, ProductVersion = version, Installed = install != null, Status = status, IsLicensed = null, LicenseType = method, Confidence = registration == null ? ConfidenceLevel.Low : ConfidenceLevel.Medium, Evidence = facts, Warnings = warnings.Distinct().ToArray() };
        }
    }
}
