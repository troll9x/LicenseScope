using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;
using LicenseScope.Office.Acquisition;
using LicenseScope.Office.Mapping;

namespace LicenseScope.Office
{
    public sealed class OfficeLicenseScanner : ILicenseScanner
    {
        private readonly IOfficeEvidenceCollector _collector; private readonly OfficeLicenseResultFactory _factory;
        public OfficeLicenseScanner(IOfficeEvidenceCollector collector, OfficeLicenseResultFactory factory) { _collector = collector ?? throw new ArgumentNullException(nameof(collector)); _factory = factory ?? throw new ArgumentNullException(nameof(factory)); }
        public string ScannerId => "microsoft.office";
        public string VendorName => "Microsoft";
        public bool IsApplicable(SystemContext context) => context != null && (context.OsName.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0 || !string.IsNullOrWhiteSpace(context.WindowsDirectory));
        public async Task<IReadOnlyList<LicenseResult>> ScanAsync(SystemContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested(); var evidence = await _collector.CollectAsync(context, cancellationToken).ConfigureAwait(false); var results = new List<LicenseResult>();
            foreach (var product in evidence.Products) { cancellationToken.ThrowIfCancellationRequested(); results.Add(_factory.Create(product, evidence.Warnings)); }
            return results;
        }
    }
}
