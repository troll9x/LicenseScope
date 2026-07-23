using LicenseScope.Core.Contracts;
using LicenseScope.Office.Acquisition;
using LicenseScope.Office.Classification;
using LicenseScope.Office.Detection;
using LicenseScope.Office.Mapping;
using LicenseScope.Office.Parsing;

namespace LicenseScope.Office
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
