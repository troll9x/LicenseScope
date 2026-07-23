using System; using System.Collections.Generic; using System.Linq; using LicenseScope.Core.Models; using LicenseScope.Adobe.Models;
namespace LicenseScope.Adobe.Classification
{
 public static class AdobeLicenseClassifier
 {
  public static string Model(string mode){if(mode.Equals("SHARED_DEVICE",StringComparison.OrdinalIgnoreCase)||mode.Equals("DEVICE",StringComparison.OrdinalIgnoreCase))return "SharedDevice";if(mode.Equals("NAMED_USER_EDUCATION_LAB",StringComparison.OrdinalIgnoreCase))return "NamedUserEducationLab";if(mode.Equals("FRL_CONNECTED",StringComparison.OrdinalIgnoreCase))return "FeatureRestrictedConnected";if(mode.Equals("FRL_ISOLATED",StringComparison.OrdinalIgnoreCase)||mode.Equals("FRL_OFFLINE",StringComparison.OrdinalIgnoreCase))return "FeatureRestrictedIsolated";return "Unknown";}
  public static LicenseStatus Status(AdobeLicenseRecord record,DateTimeOffset now){var model=Model(record.DeploymentMode);if(model=="SharedDevice"&&record.DatesUnambiguous&&record.LicenseExpiry.HasValue)return record.LicenseExpiry.Value>now?LicenseStatus.Licensed:LicenseStatus.Expired;if(model=="NamedUserEducationLab")return LicenseStatus.NeedsOnlineVerification;if(model.StartsWith("FeatureRestricted",StringComparison.Ordinal))return record.DatesUnambiguous&&record.LicenseExpiry.HasValue?(record.LicenseExpiry.Value>now?LicenseStatus.Licensed:LicenseStatus.Expired):LicenseStatus.Unknown;return LicenseStatus.Unknown;}
 }
 public static class AdobeProductMatcher
 {
  public static AdobeInstalledProduct? Match(AdobeLicenseRecord record,IEnumerable<AdobeInstalledProduct>products){var exact=products.Where(x=>!string.IsNullOrWhiteSpace(x.AppId)&&string.Equals(x.AppId,record.AppId,StringComparison.OrdinalIgnoreCase)).ToArray();return exact.Length==1?exact[0]:null;}
 }
}
