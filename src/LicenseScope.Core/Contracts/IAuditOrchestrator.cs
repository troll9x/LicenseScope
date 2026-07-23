using System;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Models;

namespace LicenseScope.Core.Contracts
{
    /// <summary>Coordinates all registered scanners without depending on a UI framework.</summary>
    public interface IAuditOrchestrator
    {
        Task<AuditResult> RunAllAsync(SystemContext context, CancellationToken cancellationToken, IProgress<AuditProgress>? progress = null);
    }
}
