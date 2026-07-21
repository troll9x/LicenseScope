using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinLic.Core.Contracts;
using WinLic.Core.Models;
using WinLic.Core.Runtime;
using WinLic.Core.Services;
using WinLic.Scanners.Office;
using WinLic.Scanners.Windows;

namespace WinLic.Application
{
    public interface IUnifiedAuditService { Task<AuditResult> RunAllAsync(CancellationToken cancellationToken, IProgress<AuditProgress>? progress = null); }

    public static class ProductionScannerFactory
    {
        public static IReadOnlyList<ILicenseScanner> CreateAll(IProcessRunner runner, ISystemClock clock)
        {
            var scanners = new ILicenseScanner[] { WindowsScannerFactory.Create(runner, clock), OfficeScannerFactory.Create(runner) };
            ValidateUnique(scanners); return scanners;
        }
        public static void ValidateUnique(IEnumerable<ILicenseScanner> scanners)
        {
            if (scanners == null) throw new ArgumentNullException(nameof(scanners));
            var duplicate = scanners.GroupBy(x => x.ScannerId, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Key) || x.Count() > 1);
            if (duplicate != null) throw new ArgumentException("Scanner IDs must be non-empty and unique.", nameof(scanners));
        }
    }

    public sealed class UnifiedAuditService : IUnifiedAuditService
    {
        private readonly IAuditOrchestrator _orchestrator; private readonly ISystemContextProvider _context;
        public UnifiedAuditService(IAuditOrchestrator orchestrator, ISystemContextProvider context) { _orchestrator = orchestrator; _context = context; }
        public Task<AuditResult> RunAllAsync(CancellationToken cancellationToken, IProgress<AuditProgress>? progress = null) => _orchestrator.RunAllAsync(_context.GetCurrent(), cancellationToken, progress);
        public static UnifiedAuditService CreateProduction()
        {
            var clock = new SystemClock(); var runner = new ProcessRunner();
            return new UnifiedAuditService(new AuditOrchestrator(ProductionScannerFactory.CreateAll(runner, clock), clock), new DefaultSystemContextProvider());
        }
    }
}
