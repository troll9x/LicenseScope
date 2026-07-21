using WinLic.Core.Contracts;
using WinLic.Scanners.Office.Acquisition;
using WinLic.Scanners.Office.Classification;
using WinLic.Scanners.Office.Detection;
using WinLic.Scanners.Office.Mapping;
using WinLic.Scanners.Office.Parsing;

namespace WinLic.Scanners.Office
{
    public static class OfficeScannerFactory
    {
        public static OfficeLicenseScanner Create(IProcessRunner runner)
        {
            var osppParser = new OsppStatusParser(); var vnextParser = new VNextStatusParser();
            var collector = new OfficeEvidenceCollector(new OfficeInstallationDetector(), new OfficeToolLocator(), new OsppEvidenceProvider(runner), new VNextEvidenceProvider(runner), osppParser, vnextParser);
            return new OfficeLicenseScanner(collector, new OfficeLicenseResultFactory(new OfficeLicenseClassifier()));
        }
    }
}
