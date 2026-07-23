using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;

namespace LicenseScope.Core.Services
{
    /// <summary>Runs scanners sequentially, preserving completed results and isolating scanner failures.</summary>
    public sealed class AuditOrchestrator : IAuditOrchestrator
    {
        private readonly IReadOnlyList<ILicenseScanner> _scanners;
        private readonly ISystemClock _clock;

        public AuditOrchestrator(IEnumerable<ILicenseScanner> scanners, ISystemClock clock)
        {
            if (scanners == null) throw new ArgumentNullException(nameof(scanners));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _scanners = scanners.ToArray();
            if (_scanners.Any(s => s == null)) throw new ArgumentException("Scanner collection cannot contain null.", nameof(scanners));
        }

        public async Task<AuditResult> RunAllAsync(SystemContext context, CancellationToken cancellationToken, IProgress<AuditProgress>? progress = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var startedAt = _clock.UtcNow;
            var products = new List<LicenseResult>();
            var executions = new List<ScannerExecutionResult>();
            var wasCancelled = false;

            for (var index = 0; index < _scanners.Count; index++)
            {
                if (cancellationToken.IsCancellationRequested) { wasCancelled = true; break; }
                var scanner = _scanners[index];
                var execution = new ScannerExecutionResult { ScannerId = scanner.ScannerId ?? string.Empty, StartedAt = _clock.UtcNow };
                executions.Add(execution);
                progress?.Report(CreateProgress(scanner, index, "Checking applicability"));

                try
                {
                    execution.WasApplicable = scanner.IsApplicable(context);
                    if (!execution.WasApplicable)
                    {
                        execution.WasSuccessful = true;
                        continue;
                    }

                    progress?.Report(CreateProgress(scanner, index, "Scanning"));
                    var scannerProducts = await scanner.ScanAsync(context, cancellationToken).ConfigureAwait(false);
                    if (scannerProducts == null) throw new InvalidOperationException("Scanner returned a null result collection.");
                    products.AddRange(scannerProducts);
                    execution.ProductResultCount = scannerProducts.Count;
                    execution.WasSuccessful = true;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    execution.WasCancelled = true;
                    execution.ErrorType = typeof(OperationCanceledException).FullName ?? nameof(OperationCanceledException);
                    execution.ErrorMessage = "Scanner execution was cancelled.";
                    wasCancelled = true;
                    break;
                }
                catch (Exception ex)
                {
                    execution.ErrorType = ex.GetType().FullName ?? ex.GetType().Name;
                    execution.ErrorMessage = ex.Message;
                }
                finally
                {
                    execution.CompletedAt = _clock.UtcNow;
                }
            }

            return new AuditResult
            {
                System = context,
                StartedAt = startedAt,
                CompletedAt = _clock.UtcNow,
                WasCancelled = wasCancelled,
                Products = products,
                ScannerExecutions = executions
            };
        }

        private AuditProgress CreateProgress(ILicenseScanner scanner, int zeroBasedIndex, string message)
        {
            return new AuditProgress
            {
                ScannerId = scanner.ScannerId ?? string.Empty,
                ScannerName = scanner.VendorName ?? string.Empty,
                CurrentIndex = zeroBasedIndex + 1,
                TotalScannerCount = _scanners.Count,
                Message = message,
                Percentage = _scanners.Count == 0 ? (double?)null : (zeroBasedIndex * 100d) / _scanners.Count
            };
        }
    }
}
