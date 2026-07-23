using LicenseScope.Core.Contracts;
using LicenseScope.Core.Runtime;
using LicenseScope.Windows.Acquisition;
using LicenseScope.Windows.Classification;
using LicenseScope.Windows.Mapping;
using LicenseScope.Windows.Models;
using LicenseScope.Windows.Parsing;

namespace LicenseScope.Windows
{
    public static class WindowsScannerFactory
    {
        public static WindowsLicenseScanner Create(
            IProcessRunner processRunner,
            ISystemClock clock,
            IWindowsKnownKeyCatalog? knownKeyCatalog = null,
            WindowsInspectionSettings? inspectionSettings = null)
        {
            var xpr = new SlmgrXprParser();
            var dlv = new SlmgrDlvParser();
            var slmgr = new WindowsSlmgrEvidenceProvider(processRunner, xpr, dlv);
            var collector = new WindowsEvidenceCollector(new WindowsWmiQueryService(), new WindowsRegistryReader(), slmgr);
            var classifier = new WindowsLicenseClassifier(
                new WindowsChannelClassifier(),
                knownKeyCatalog ?? new WindowsKnownKeyCatalog(),
                clock);
            var inspector = inspectionSettings == null ? null : new WindowsReadOnlyInspector(processRunner, inspectionSettings);
            return new WindowsLicenseScanner(
                collector,
                new WindowsProductSelector(),
                classifier,
                new WindowsLicenseResultFactory(),
                inspector);
        }

        public static WindowsLicenseScanner CreateDefault() => Create(new ProcessRunner(), new LicenseScope.Core.Services.SystemClock());
    }
}
