using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;
using LicenseScope.Core.Runtime;
using LicenseScope.Core.Services;
using LicenseScope.Office;
using LicenseScope.Autodesk;
using LicenseScope.Adobe;
using LicenseScope.SketchUp;
using LicenseScope.Windows;
using LicenseScope.Windows.Classification;
using LicenseScope.Windows.Models;

namespace LicenseScope.Application
{
    public interface IUnifiedAuditService { Task<AuditResult> RunAllAsync(CancellationToken cancellationToken, IProgress<AuditProgress>? progress = null); }

    public static class ProductionScannerFactory
    {
        public static IReadOnlyList<ILicenseScanner> CreateAll(
            IProcessRunner runner,
            ISystemClock clock,
            IWindowsKnownKeyCatalog? knownKeyCatalog = null,
            WindowsInspectionSettings? inspectionSettings = null)
        {
            var scanners = new ILicenseScanner[] { WindowsScannerFactory.Create(runner, clock, knownKeyCatalog, inspectionSettings), OfficeScannerFactory.Create(runner), AutodeskScannerFactory.Create(runner), AdobeScannerFactory.Create(runner, clock), SketchUpScannerFactory.Create() };
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
        private readonly IAuditOrchestrator _orchestrator;
        private readonly ISystemContextProvider _context;
        private readonly ICrackTraceAnalyzer? _crackTraceAnalyzer;
        public UnifiedAuditService(
            IAuditOrchestrator orchestrator,
            ISystemContextProvider context,
            ICrackTraceAnalyzer? crackTraceAnalyzer = null)
        {
            _orchestrator = orchestrator;
            _context = context;
            _crackTraceAnalyzer = crackTraceAnalyzer;
        }
        public async Task<AuditResult> RunAllAsync(
            CancellationToken cancellationToken,
            IProgress<AuditProgress>? progress = null)
        {
            var context = _context.GetCurrent();
            var result = await _orchestrator.RunAllAsync(context, cancellationToken, progress)
                .ConfigureAwait(false);
            if (!result.WasCancelled && _crackTraceAnalyzer != null)
                result.CrackTraceAnalysis = await _crackTraceAnalyzer
                    .AnalyzeAsync(context, cancellationToken)
                    .ConfigureAwait(false);
            return result;
        }
        public static UnifiedAuditService CreateProduction(
            IWindowsKnownKeyCatalog? knownKeyCatalog = null,
            WindowsInspectionSettings? inspectionSettings = null)
        {
            var clock = new SystemClock(); var runner = new ProcessRunner();
            return new UnifiedAuditService(
                new AuditOrchestrator(
                    ProductionScannerFactory.CreateAll(runner, clock, knownKeyCatalog, inspectionSettings),
                    clock),
                new DefaultSystemContextProvider(),
                CrackTraceAnalyzerFactory.Create(runner));
        }
    }
}
