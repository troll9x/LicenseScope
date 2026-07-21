using System.Threading;
using System.Threading.Tasks;
using WinLic.Core.Models;
using WinLic.Scanners.Windows.Models;

namespace WinLic.Scanners.Windows.Acquisition
{
    public interface IWindowsEvidenceCollector
    {
        Task<WindowsLicenseEvidence> CollectAsync(SystemContext context, CancellationToken cancellationToken);
    }
}
