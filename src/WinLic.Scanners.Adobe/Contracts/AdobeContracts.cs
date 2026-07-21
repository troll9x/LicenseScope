using System.Collections.Generic; using System.Threading; using System.Threading.Tasks; using WinLic.Core.Models; using WinLic.Scanners.Adobe.Models;
namespace WinLic.Scanners.Adobe.Contracts
{
 public interface IAdobeInstallationDetector { IReadOnlyList<AdobeInstalledProduct> Detect(SystemContext context,out IReadOnlyList<string>warnings); }
 public interface IAdobeLicensingToolkitLocator { AdobeToolkitInfo Locate(); }
 public interface IAdobeLicensingEvidenceProvider { Task<AdobeLicenseInformationResult> ReadLicenseInformationAsync(CancellationToken cancellationToken); }
 public interface IAdobeSharedDeviceConfigurationDetector { AdobeSharedDeviceConfiguration Detect(); }
 public interface IAdobeServiceStatusProvider { AdobeServiceStatus GetStatus(); }
}
