using WinLic.Scanners.SketchUp.Acquisition;using WinLic.Scanners.SketchUp.Detection;
namespace WinLic.Scanners.SketchUp{public static class SketchUpScannerFactory{public static SketchUpLicenseScanner Create()=>new SketchUpLicenseScanner(new SketchUpInstallationDetector(),new SketchUpSubscriptionEvidenceProvider(),new SketchUpClassicEvidenceProvider());}}
