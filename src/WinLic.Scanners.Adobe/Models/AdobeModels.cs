using System; using System.Collections.Generic;
namespace WinLic.Scanners.Adobe.Models
{
 public sealed class AdobeInstalledProduct { public string ProductCode{get;set;}=""; public string AppId{get;set;}=""; public string ProductName{get;set;}=""; public string Version{get;set;}=""; public string ReleaseYear{get;set;}=""; public string Architecture{get;set;}=""; public string InstallLocation{get;set;}=""; public string DetectionSource{get;set;}=""; }
 public sealed class AdobeToolkitInfo { public string Path{get;set;}=""; public string Version{get;set;}=""; public bool Exists{get;set;} }
 public sealed class AdobeLicenseRecord { public string AppId{get;set;}=""; public string DeploymentMode{get;set;}=""; public DateTimeOffset? CacheExpiry{get;set;} public DateTimeOffset? LicenseExpiry{get;set;} public bool NpdIdPresent{get;set;} public bool LicenseIdPresent{get;set;} public bool DatesUnambiguous{get;set;} }
 public sealed class AdobeLicenseInformationResult { public bool Successful{get;set;} public IReadOnlyList<AdobeLicenseRecord> Records{get;set;}=Array.Empty<AdobeLicenseRecord>(); public IReadOnlyList<string> Warnings{get;set;}=Array.Empty<string>(); }
 public sealed class AdobeSharedDeviceConfiguration { public bool Present{get;set;} public int Count{get;set;} public string Warning{get;set;}=""; }
 public sealed class AdobeServiceStatus { public bool Found{get;set;} public bool Running{get;set;} public string Warning{get;set;}=""; }
}
