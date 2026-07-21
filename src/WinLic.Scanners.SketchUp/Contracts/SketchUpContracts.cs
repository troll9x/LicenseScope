using System.Collections.Generic;using System.Threading;using WinLic.Core.Models;using WinLic.Scanners.SketchUp.Models;
namespace WinLic.Scanners.SketchUp.Contracts
{
 public interface ISketchUpInstallationDetector{IReadOnlyList<SketchUpInstalledProduct>Detect(SystemContext context,out IReadOnlyList<string>warnings);}
 public interface ISketchUpSubscriptionEvidenceProvider{SketchUpSubscriptionEvidence Detect(SketchUpInstalledProduct product,CancellationToken token);}
 public interface ISketchUpClassicEvidenceProvider{SketchUpClassicEvidence Detect(SketchUpInstalledProduct product,CancellationToken token);}
}
