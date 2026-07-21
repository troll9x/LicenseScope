using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinLic.Core.Contracts;
using WinLic.Core.Models;
using WinLic.Scanners.Windows.Acquisition;
using WinLic.Scanners.Windows.Classification;
using WinLic.Scanners.Windows.Mapping;

namespace WinLic.Scanners.Windows
{
    public sealed class WindowsLicenseScanner : ILicenseScanner
    {
        private readonly IWindowsEvidenceCollector _collector;
        private readonly WindowsProductSelector _selector;
        private readonly WindowsLicenseClassifier _classifier;
        private readonly WindowsLicenseResultFactory _factory;
        public WindowsLicenseScanner(IWindowsEvidenceCollector collector, WindowsProductSelector selector, WindowsLicenseClassifier classifier, WindowsLicenseResultFactory factory)
        {
            _collector = collector;
            _selector = selector;
            _classifier = classifier;
            _factory = factory;
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
            return new[] { _factory.Create(context, selected.Product, evidence, classification) };
        }
    }
}
