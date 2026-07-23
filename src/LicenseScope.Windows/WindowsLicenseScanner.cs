using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;
using LicenseScope.Windows.Acquisition;
using LicenseScope.Windows.Classification;
using LicenseScope.Windows.Mapping;

namespace LicenseScope.Windows
{
    public sealed class WindowsLicenseScanner : ILicenseScanner
    {
        private readonly IWindowsEvidenceCollector _collector;
        private readonly WindowsProductSelector _selector;
        private readonly WindowsLicenseClassifier _classifier;
        private readonly WindowsLicenseResultFactory _factory;
        private readonly WindowsReadOnlyInspector? _inspector;
        public WindowsLicenseScanner(
            IWindowsEvidenceCollector collector,
            WindowsProductSelector selector,
            WindowsLicenseClassifier classifier,
            WindowsLicenseResultFactory factory,
            WindowsReadOnlyInspector? inspector = null)
        {
            _collector = collector;
            _selector = selector;
            _classifier = classifier;
            _factory = factory;
            _inspector = inspector;
        }
        public string ScannerId => "microsoft.windows";
        public string VendorName => "Microsoft";
        public bool IsApplicable(SystemContext context) => context != null && (context.OsName.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0 || context.WindowsDirectory.Length > 0);

        public async Task<IReadOnlyList<LicenseResult>> ScanAsync(SystemContext context, CancellationToken cancellationToken)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            cancellationToken.ThrowIfCancellationRequested();
            var evidence = await _collector.CollectAsync(context, cancellationToken).ConfigureAwait(false);
            var selected = _selector.Select(evidence.Products);
            var classification = _classifier.Classify(selected.Product, evidence, selected.Ambiguous);
            var result = _factory.Create(context, selected.Product, evidence, classification);
            if (_inspector != null)
            {
                var inspection = await _inspector.InspectAsync(context, selected.Product, cancellationToken).ConfigureAwait(false);
                result.Evidence = result.Evidence.Concat(inspection.Evidence).ToArray();
                result.Warnings = result.Warnings.Concat(inspection.Warnings)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }
            return new[] { result };
        }
    }
}
