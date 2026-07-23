using LicenseScope.Core.Models;using LicenseScope.SketchUp.Models;
namespace LicenseScope.SketchUp.Classification
{
 public static class SketchUpLicenseClassifier
 {
  public static string Type(SketchUpInstalledProduct product,SketchUpSubscriptionEvidence subscription,SketchUpClassicEvidence classic){if(classic.NetworkConfigurationPresent)return "ClassicNetwork";if(classic.ClassicArtifactPresent)return "ClassicSingleUser";if(subscription.SessionArtifactPresent||subscription.VersionProfilePresent||int.TryParse(product.ReleaseYear,out var year)&&year>=2021)return "Subscription";return "Unknown";}
  public static LicenseStatus Status(string type){if(type=="Subscription"||type=="ClassicNetwork")return LicenseStatus.NeedsOnlineVerification;return LicenseStatus.Unknown;}
 }
}
