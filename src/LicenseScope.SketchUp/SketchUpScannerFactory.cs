using LicenseScope.SketchUp.Acquisition;using LicenseScope.SketchUp.Detection;
namespace LicenseScope.SketchUp{public static class SketchUpScannerFactory{public static SketchUpLicenseScanner Create()=>new SketchUpLicenseScanner(new SketchUpInstallationDetector(),new SketchUpSubscriptionEvidenceProvider(),new SketchUpClassicEvidenceProvider());}}
