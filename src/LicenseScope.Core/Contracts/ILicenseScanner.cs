using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Models;

namespace LicenseScope.Core.Contracts
{
    /// <summary>Contract implemented by a stable, independently failing license scanner.</summary>
    public interface ILicenseScanner
    {
        string ScannerId { get; }
        string VendorName { get; }
        bool IsApplicable(SystemContext context);
        Task<IReadOnlyList<LicenseResult>> ScanAsync(SystemContext context, CancellationToken cancellationToken);
    }
}
