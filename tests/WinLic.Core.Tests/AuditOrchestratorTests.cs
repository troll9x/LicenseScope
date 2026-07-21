using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinLic.Core.Contracts;
using WinLic.Core.Models;
using WinLic.Core.Services;

namespace WinLic.Core.Tests
{
    [TestClass]
    public sealed class AuditOrchestratorTests
    {
        [TestMethod]
        public async Task NoScannersProducesEmptyCompletedAudit()
        {
            var result = await Create().RunAllAsync(new SystemContext(), CancellationToken.None);
            Assert.AreEqual(0, result.Products.Count);
            Assert.AreEqual(0, result.ScannerExecutions.Count);
            Assert.IsFalse(result.WasCancelled);
        }

        [TestMethod]
        public async Task AggregatesSuccessfulAndEmptyScanners()
        {
            var result = await Create(new FakeScanner("one", true, Product()), new FakeScanner("empty", true, Array.Empty<LicenseResult>()))
                .RunAllAsync(new SystemContext(), CancellationToken.None);
            Assert.AreEqual(1, result.Products.Count);
            Assert.AreEqual(2, result.ScannerExecutions.Count);
            Assert.IsTrue(result.ScannerExecutions[0].WasSuccessful);
            Assert.AreEqual(0, result.ScannerExecutions[1].ProductResultCount);
        }

        [TestMethod]
        public async Task RecordsNotApplicableScannerWithoutRunningIt()
        {
            var scanner = new FakeScanner("skip", false, Product());
            var result = await Create(scanner).RunAllAsync(new SystemContext(), CancellationToken.None);
            Assert.IsFalse(result.ScannerExecutions[0].WasApplicable);
            Assert.IsTrue(result.ScannerExecutions[0].WasSuccessful);
            Assert.AreEqual(0, scanner.ScanCalls);
        }

        [TestMethod]
        public async Task ExceptionIsIsolatedAndLaterScannerRuns()
        {
            var later = new FakeScanner("later", true, Product());
            var result = await Create(new FakeScanner("broken", new InvalidOperationException("fixture failure")), later)
                .RunAllAsync(new SystemContext(), CancellationToken.None);
            Assert.IsFalse(result.ScannerExecutions[0].WasSuccessful);
            Assert.AreEqual(typeof(InvalidOperationException).FullName, result.ScannerExecutions[0].ErrorType);
            Assert.AreEqual("fixture failure", result.ScannerExecutions[0].ErrorMessage);
            Assert.AreEqual(1, later.ScanCalls);
            Assert.AreEqual(1, result.Products.Count);
        }

        [TestMethod]
        public async Task NullScannerResultIsRecordedAsFailure()
        {
            var scanner = new FakeScanner("null", _ => Task.FromResult<IReadOnlyList<LicenseResult>>(null!));
            var result = await Create(scanner).RunAllAsync(new SystemContext(), CancellationToken.None);
            Assert.IsFalse(result.ScannerExecutions[0].WasSuccessful);
            Assert.AreEqual(typeof(InvalidOperationException).FullName, result.ScannerExecutions[0].ErrorType);
        }

        [TestMethod]
        public async Task ApplicabilityExceptionIsRecordedAndLaterScannerRuns()
        {
            var broken = new FakeScanner("applicability", true, Product()) { ApplicabilityError = new InvalidOperationException("applicability failure") };
            var later = new FakeScanner("later", true, Product());
            var result = await Create(broken, later).RunAllAsync(new SystemContext(), CancellationToken.None);
            Assert.AreEqual("applicability failure", result.ScannerExecutions[0].ErrorMessage);
            Assert.AreEqual(1, later.ScanCalls);
        }

        [TestMethod]
        public async Task CancellationIsStructuredAndStopsLaterScanners()
        {
            var source = new CancellationTokenSource();
            var cancelling = new FakeScanner("cancel", async token =>
            {
                source.Cancel();
                await Task.Yield();
                token.ThrowIfCancellationRequested();
                return Array.Empty<LicenseResult>();
            });
            var later = new FakeScanner("later", true, Product());
            var result = await Create(cancelling, later).RunAllAsync(new SystemContext(), source.Token);
            Assert.IsTrue(result.WasCancelled);
            Assert.IsTrue(result.ScannerExecutions[0].WasCancelled);
            Assert.AreEqual(0, later.ScanCalls);
        }

        [TestMethod]
        public async Task ResultsCompletedBeforeCancellationAreRetained()
        {
            var source = new CancellationTokenSource();
            var cancelling = new FakeScanner("cancel", token =>
            {
                source.Cancel();
                token.ThrowIfCancellationRequested();
                return Task.FromResult<IReadOnlyList<LicenseResult>>(Array.Empty<LicenseResult>());
            });
            var result = await Create(new FakeScanner("first", true, Product()), cancelling)
                .RunAllAsync(new SystemContext(), source.Token);
            Assert.AreEqual(1, result.Products.Count);
            Assert.IsTrue(result.WasCancelled);
        }

        [TestMethod]
        public async Task ReportsProgressAndUsesInjectedClock()
        {
            var progress = new CaptureProgress();
            var clock = new FakeClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var result = await new AuditOrchestrator(new[] { new FakeScanner("one", true, Product()) }, clock)
                .RunAllAsync(new SystemContext(), CancellationToken.None, progress);
            Assert.IsTrue(progress.Items.Count >= 2);
            Assert.AreEqual("one", progress.Items[0].ScannerId);
            Assert.IsTrue(result.CompletedAt >= result.StartedAt);
            Assert.IsTrue(result.ScannerExecutions[0].CompletedAt >= result.ScannerExecutions[0].StartedAt);
        }

        private static AuditOrchestrator Create(params ILicenseScanner[] scanners) => new AuditOrchestrator(scanners, new FakeClock(DateTimeOffset.UtcNow));

        private static LicenseResult[] Product() => new[] { new LicenseResult { ScannerId = "fixture", Vendor = "Fixture", ProductName = "Product", Installed = true } };

        private sealed class FakeScanner : ILicenseScanner
        {
            private readonly bool _applicable;
            private readonly Func<CancellationToken, Task<IReadOnlyList<LicenseResult>>> _scan;
            public FakeScanner(string id, bool applicable, IReadOnlyList<LicenseResult> results) : this(id, _ => Task.FromResult(results)) { _applicable = applicable; }
            public FakeScanner(string id, Exception error) : this(id, _ => Task.FromException<IReadOnlyList<LicenseResult>>(error)) { }
            public FakeScanner(string id, Func<CancellationToken, Task<IReadOnlyList<LicenseResult>>> scan) { ScannerId = id; VendorName = "Fixture"; _applicable = true; _scan = scan; }
            public string ScannerId { get; }
            public string VendorName { get; }
            public int ScanCalls { get; private set; }
            public Exception? ApplicabilityError { get; set; }
            public bool IsApplicable(SystemContext context)
            {
                if (ApplicabilityError != null) throw ApplicabilityError;
                return _applicable;
            }
            public Task<IReadOnlyList<LicenseResult>> ScanAsync(SystemContext context, CancellationToken cancellationToken) { ScanCalls++; return _scan(cancellationToken); }
        }

        private sealed class FakeClock : ISystemClock
        {
            private DateTimeOffset _now;
            public FakeClock(DateTimeOffset now) { _now = now; }
            public DateTimeOffset UtcNow { get { var value = _now; _now = _now.AddSeconds(1); return value; } }
        }

        private sealed class CaptureProgress : IProgress<AuditProgress>
        {
            public List<AuditProgress> Items { get; } = new List<AuditProgress>();
            public void Report(AuditProgress value) => Items.Add(value);
        }
    }
}
