using LicenseScope.Core.Contracts;
using LicenseScope.Autodesk.Acquisition;
using LicenseScope.Autodesk.Detection;
using LicenseScope.Autodesk.Parsing;

namespace LicenseScope.Autodesk
{
    public static class AutodeskScannerFactory
    {
        public static AutodeskLicenseScanner Create(IProcessRunner runner) { var locator = new AutodeskLicensingToolLocator(); return new AutodeskLicenseScanner(new AutodeskInstallationDetector(), new AutodeskLicensingEvidenceProvider(runner, locator, new AdskLicensingListParser()), new AutodeskServiceStatusProvider()); }
    }
}
