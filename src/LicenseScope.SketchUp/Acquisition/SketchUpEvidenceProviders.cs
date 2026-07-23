using System;using System.IO;using System.Threading;using LicenseScope.SketchUp.Contracts;using LicenseScope.SketchUp.Models;
namespace LicenseScope.SketchUp.Acquisition
{
 public sealed class SketchUpSubscriptionEvidenceProvider:ISketchUpSubscriptionEvidenceProvider
 {
  public SketchUpSubscriptionEvidence Detect(SketchUpInstalledProduct product,CancellationToken token){token.ThrowIfCancellationRequested();try{var roaming=Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);var profile=Path.Combine(roaming,"SketchUp","SketchUp "+product.ReleaseYear);var session=Path.Combine(profile,"login_session.dat");var present=File.Exists(session);return new SketchUpSubscriptionEvidence{VersionProfilePresent=Directory.Exists(profile),SessionArtifactPresent=present,SessionArtifactLastWriteTime=present?new DateTimeOffset(File.GetLastWriteTimeUtc(session),TimeSpan.Zero):(DateTimeOffset?)null};}catch(Exception ex)when(ex is UnauthorizedAccessException||ex is IOException){return new SketchUpSubscriptionEvidence{Warning="SketchUp subscription artifact metadata unavailable: "+ex.GetType().Name};}}
 }
 public sealed class SketchUpClassicEvidenceProvider:ISketchUpClassicEvidenceProvider
 {
  public SketchUpClassicEvidence Detect(SketchUpInstalledProduct product,CancellationToken token){token.ThrowIfCancellationRequested();try{var common=Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);var root=Path.Combine(common,"SketchUp","SketchUp "+product.ReleaseYear);var activation=Path.Combine(root,"activation_info.txt");var server=Path.Combine(root,"server.dat");var network=File.Exists(activation)||File.Exists(server);return new SketchUpClassicEvidence{ClassicArtifactPresent=network,NetworkConfigurationPresent=network};}catch(Exception ex)when(ex is UnauthorizedAccessException||ex is IOException){return new SketchUpClassicEvidence{Warning="SketchUp Classic artifact metadata unavailable: "+ex.GetType().Name};}}
 }
}
