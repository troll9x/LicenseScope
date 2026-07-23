using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Models;
using LicenseScope.Windows.Models;

namespace LicenseScope.Windows.Acquisition
{
    public interface ICrackTraceEvidenceSource
    {
        Task<CrackTraceEvidenceSnapshot> CollectAsync(
            SystemContext context,
            CancellationToken cancellationToken);
    }
}
