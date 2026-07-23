using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Models;
using LicenseScope.Autodesk.Models;

namespace LicenseScope.Autodesk.Contracts
{
    public interface IAutodeskInstallationDetector { IReadOnlyList<AutodeskInstallation> Detect(SystemContext context, out IReadOnlyList<string> warnings); }
    public interface IAutodeskLicensingToolLocator { AutodeskToolInfo Locate(); }
    public interface IAutodeskLicensingEvidenceProvider { Task<AutodeskEvidenceResult> ListAsync(CancellationToken cancellationToken); }
    public interface IAutodeskServiceStatusProvider { AutodeskServiceStatus GetStatus(); }
}
