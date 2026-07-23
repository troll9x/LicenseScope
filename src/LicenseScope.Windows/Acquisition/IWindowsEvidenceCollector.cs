using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Models;
using LicenseScope.Windows.Models;

namespace LicenseScope.Windows.Acquisition
{
    public interface IWindowsEvidenceCollector
    {
        Task<WindowsLicenseEvidence> CollectAsync(SystemContext context, CancellationToken cancellationToken);
    }
}
