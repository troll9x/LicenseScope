using WinLic.Core.Contracts;
using WinLic.Scanners.Autodesk.Acquisition;
using WinLic.Scanners.Autodesk.Detection;
using WinLic.Scanners.Autodesk.Parsing;

namespace WinLic.Scanners.Autodesk
{
    public static class AutodeskScannerFactory
    {
        public static AutodeskLicenseScanner Create(IProcessRunner runner) { var locator = new AutodeskLicensingToolLocator(); return new AutodeskLicenseScanner(new AutodeskInstallationDetector(), new AutodeskLicensingEvidenceProvider(runner, locator, new AdskLicensingListParser()), new AutodeskServiceStatusProvider()); }
    }
}
