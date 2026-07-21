using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinLic.Core.Contracts;
using WinLic.Core.Models;
using WinLic.Scanners.Windows.Acquisition;
using WinLic.Scanners.Windows.Classification;
using WinLic.Scanners.Windows.Mapping;
using WinLic.Scanners.Windows.Models;

namespace WinLic.Scanners.Windows.Tests
{
    [TestClass]
    public sealed class WindowsLicenseScannerTests
    {
        [TestMethod]
        public async Task ProducesStructuredMaskedResult()
        {
            var scanner = Create(Evidence(Product(1, "RETAIL", "3V66T")));
            var results = await scanner.ScanAsync(Context(), CancellationToken.None);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("microsoft.windows", results[0].ScannerId);
            Assert.AreEqual(LicenseStatus.Licensed, results[0].Status);
            Assert.AreEqual("XXXXX-XXXXX-XXXXX-XXXXX-3V66T", results[0].PartialProductKey);
            Assert.IsFalse(results.SelectMany(r => r.Evidence).Any(e => e.Value.Contains("AAAAA-BBBBB")));
        }

        [TestMethod]
        public async Task AllSourcesFailedReturnsUnknownNotCrash()
        {
            var result = (await Create(new WindowsLicenseEvidence { Warnings = new[] { "WMI unavailable", "slmgr unavailable" } }).ScanAsync(Context(), CancellationToken.None))[0];
            Assert.AreEqual(LicenseStatus.Unknown, result.Status);
            Assert.IsNull(result.IsLicensed);
            Assert.AreEqual(2, result.Warnings.Count);
        }

        [TestMethod]
        public async Task MultipleCandidatesAddWarning()
        {
            var evidence = Evidence(Product(1, "RETAIL", "AAAAA"), Product(1, "RETAIL", "BBBBB"));
            var result = (await Create(evidence).ScanAsync(Context(), CancellationToken.None))[0];
            StringAssert.Contains(string.Join(" ", result.Warnings), "Multiple");
        }

        [TestMethod]
        public async Task CancellationIsPropagated()
        {
            var source = new CancellationTokenSource(); source.Cancel();
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => Create(Evidence()).ScanAsync(Context(), source.Token));
        }

        [TestMethod]
        public void IsApplicableIsConservative()
        {
            var scanner = Create(Evidence());
            Assert.IsTrue(scanner.IsApplicable(Context()));
            Assert.IsFalse(scanner.IsApplicable(new SystemContext { OsName = "Linux" }));
        }

        private static WindowsLicenseScanner Create(WindowsLicenseEvidence evidence) => new WindowsLicenseScanner(new FakeCollector(evidence), new WindowsProductSelector(), new WindowsLicenseClassifier(new WindowsChannelClassifier(), new WindowsKnownKeyCatalog(), new FixedClock()), new WindowsLicenseResultFactory());
        private static WindowsLicenseEvidence Evidence(params WindowsLicenseProductRecord[] products) => new WindowsLicenseEvidence { Products = products, Xpr = new SlmgrXprParseResult { Parsed = true, IsPermanent = true } };
        private static WindowsLicenseProductRecord Product(uint status, string description, string key) => new WindowsLicenseProductRecord { ApplicationId = WindowsEvidenceCollector.WindowsApplicationId, Name = "Windows 11 Pro", LicenseStatus = status, Description = description, PartialProductKey = key };
        private static SystemContext Context() => new SystemContext { OsName = "Microsoft Windows 11 Pro", OsVersion = "10.0.26100", WindowsDirectory = @"C:\Windows" };
        private sealed class FakeCollector : IWindowsEvidenceCollector { private readonly WindowsLicenseEvidence _value; public FakeCollector(WindowsLicenseEvidence value) { _value = value; } public Task<WindowsLicenseEvidence> CollectAsync(SystemContext context, CancellationToken token) { token.ThrowIfCancellationRequested(); return Task.FromResult(_value); } }
        private sealed class FixedClock : ISystemClock { public DateTimeOffset UtcNow => new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero); }
    }
}
