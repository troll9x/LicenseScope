using System;
namespace LicenseScope.SketchUp.Models
{
 public sealed class SketchUpInstalledProduct{public string ProductName{get;set;}="";public string Version{get;set;}="";public string ReleaseYear{get;set;}="";public string Architecture{get;set;}="";public string InstallLocation{get;set;}="";public string DetectionSource{get;set;}="";public bool LayOutInstalled{get;set;}public bool StyleBuilderInstalled{get;set;}}
 public sealed class SketchUpSubscriptionEvidence{public bool VersionProfilePresent{get;set;}public bool SessionArtifactPresent{get;set;}public DateTimeOffset? SessionArtifactLastWriteTime{get;set;}public string Warning{get;set;}="";}
 public sealed class SketchUpClassicEvidence{public bool ClassicArtifactPresent{get;set;}public bool NetworkConfigurationPresent{get;set;}public string Warning{get;set;}="";}
}
