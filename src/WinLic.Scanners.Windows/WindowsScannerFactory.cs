using WinLic.Core.Contracts;
using WinLic.Core.Runtime;
using WinLic.Scanners.Windows.Acquisition;
using WinLic.Scanners.Windows.Classification;
using WinLic.Scanners.Windows.Mapping;
using WinLic.Scanners.Windows.Parsing;

namespace WinLic.Scanners.Windows
{
    public static class WindowsScannerFactory
    {
        public static WindowsLicenseScanner Create(IProcessRunner processRunner, ISystemClock clock)
        {
            var xpr = new SlmgrXprParser();
            var dlv = new SlmgrDlvParser();
            var slmgr = new WindowsSlmgrEvidenceProvider(processRunner, xpr, dlv);
            var collector = new WindowsEvidenceCollector(new WindowsWmiQueryService(), new WindowsRegistryReader(), slmgr);
            var classifier = new WindowsLicenseClassifier(new WindowsChannelClassifier(), new WindowsKnownKeyCatalog(), clock);
            return new WindowsLicenseScanner(collector, new WindowsProductSelector(), classifier, new WindowsLicenseResultFactory());
        }

        public static WindowsLicenseScanner CreateDefault() => Create(new ProcessRunner(), new WinLic.Core.Services.SystemClock());
    }
}
