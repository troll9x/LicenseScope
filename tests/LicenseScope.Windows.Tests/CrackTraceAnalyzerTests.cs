using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LicenseScope.Core.Models;
using LicenseScope.Windows;
using LicenseScope.Windows.Acquisition;
using LicenseScope.Windows.Models;

namespace LicenseScope.Windows.Tests
{
    [TestClass]
    public sealed class CrackTraceAnalyzerTests
    {
        [TestMethod]
        public async Task AlwaysReturnsExactlySevenChecksInOrder()
        {
            var result = await Analyze(new CrackTraceEvidenceSnapshot());
            Assert.AreEqual(7, result.Checks.Count);
            CollectionAssert.AreEqual(
                new[] { 1, 2, 3, 4, 5, 6, 7 },
                result.Checks.Select(x => x.Order).ToArray());
            CollectionAssert.AreEqual(
                new[]
                {
                    "kms-crack", "mas-hwid", "kms38-hook", "license-logic",
                    "tool-paths", "scheduled-tasks", "registry-interference"
                },
                result.Checks.Select(x => x.Id).ToArray());
        }

        [TestMethod]
        public async Task NoBadEvidenceProducesCleanVerdict()
        {
            var result = await Analyze(new CrackTraceEvidenceSnapshot
            {
                Activation = ConsistentRetailActivation()
            });
            Assert.AreEqual(CrackTraceVerdict.Clean, result.Verdict);
            Assert.IsFalse(result.Checks.Any(x =>
                x.Status == CrackTraceStatus.Detected ||
                x.Status == CrackTraceStatus.Suspicious));
        }

        [TestMethod]
        public async Task OneWeakTaskNameProducesSuspiciousOnly()
        {
            var result = await Analyze(new CrackTraceEvidenceSnapshot
            {
                Activation = ConsistentRetailActivation(),
                Tasks = new[]
                {
                    new CrackTraceArtifact
                    {
                        Source = "ScheduledTask",
                        Name = "AutoKMS-like",
                        MatchedKeyword = "AutoKMS",
                        NameMatched = true,
                        ActionMatched = false
                    }
                }
            });
            Assert.AreEqual(CrackTraceVerdict.Suspicious, result.Verdict);
            Assert.AreEqual(
                CrackTraceStatus.Suspicious,
                result.Checks.Single(x => x.Order == 6).Status);
        }

        [TestMethod]
        public async Task TwoIndependentStrongSignalsProduceHighRisk()
        {
            var result = await Analyze(new CrackTraceEvidenceSnapshot
            {
                Activation = ConsistentRetailActivation(),
                Paths = new[]
                {
                    new CrackTraceArtifact
                    {
                        Source = "Path",
                        Name = "KMSpico",
                        Path = @"C:\Program Files\KMSpico",
                        MatchedKeyword = "KMSpico"
                    }
                },
                Tasks = new[]
                {
                    new CrackTraceArtifact
                    {
                        Source = "ScheduledTask",
                        Name = "AutoKMS",
                        Action = "AutoKMS.exe",
                        MatchedKeyword = "AutoKMS",
                        NameMatched = true,
                        ActionMatched = true
                    }
                }
            });
            Assert.AreEqual(CrackTraceVerdict.HighRisk, result.Verdict);
            Assert.IsTrue(result.Checks.Count(x =>
                x.Status == CrackTraceStatus.Detected && x.IsStrongSignal) >= 2);
        }

        [TestMethod]
        public async Task NoGenTicketAloneIsDocumentedPolicyNotCrack()
        {
            var result = await Analyze(new CrackTraceEvidenceSnapshot
            {
                Activation = ConsistentRetailActivation(),
                RegistryValues = new[]
                {
                    new CrackTraceRegistryEvidence
                    {
                        Path =
                            @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform",
                        Name = "NoGenTicket",
                        Value = "1",
                        Present = true
                    }
                }
            });
            var registry = result.Checks.Single(x => x.Order == 7);
            Assert.AreEqual(CrackTraceStatus.Clean, registry.Status);
            Assert.AreEqual(CrackTraceVerdict.Clean, result.Verdict);
        }

        [TestMethod]
        public async Task CorporateKmsHostIsNotFalsePositive()
        {
            var activation = ConsistentRetailActivation();
            activation.Channel = "Volume_KMSCLIENT";
            activation.KmsHost = "kms.corp.example";
            activation.KmsPort = 1688;
            activation.IsPermanent = false;
            activation.GracePeriodRemaining = 200000;
            var result = await Analyze(new CrackTraceEvidenceSnapshot { Activation = activation });
            Assert.AreEqual(
                CrackTraceStatus.Clean,
                result.Checks.Single(x => x.Order == 1).Status);
            Assert.AreNotEqual(CrackTraceVerdict.HighRisk, result.Verdict);
        }

        [TestMethod]
        public async Task DigitalEntitlementIsNotAutomaticallyMas()
        {
            var result = await Analyze(new CrackTraceEvidenceSnapshot
            {
                Activation = ConsistentRetailActivation()
            });
            var mas = result.Checks.Single(x => x.Order == 2);
            Assert.AreEqual(CrackTraceStatus.Clean, mas.Status);
            StringAssert.Contains(mas.Summary, "không bị coi là bất thường");
        }

        [TestMethod]
        public async Task FullProductKeyIsSanitizedFromEvidence()
        {
            var activation = ConsistentRetailActivation();
            activation.PartialProductKey = "AAAAA-BBBBB-CCCCC-DDDDD-ABCDE";
            var result = await Analyze(new CrackTraceEvidenceSnapshot { Activation = activation });
            var text = string.Join("|", result.Checks.SelectMany(x => x.Evidence));
            Assert.IsFalse(text.Contains("AAAAA-BBBBB"));
            StringAssert.Contains(text, "XXXXX-XXXXX-XXXXX-XXXXX-ABCDE");
        }

        [TestMethod]
        public async Task SourceFailureReturnsStructuredScanError()
        {
            var analyzer = new WindowsCrackTraceAnalyzer(new ThrowingSource());
            var result = await analyzer.AnalyzeAsync(
                new SystemContext { OsName = "Windows" },
                CancellationToken.None);
            Assert.AreEqual(7, result.Checks.Count);
            Assert.IsTrue(result.Checks.All(x => x.Status == CrackTraceStatus.Error));
            Assert.AreEqual(CrackTraceVerdict.ScanError, result.Verdict);
        }

        [TestMethod]
        public async Task UnavailableRegistryAndTaskDoNotCrashOtherChecks()
        {
            var result = await Analyze(new CrackTraceEvidenceSnapshot
            {
                Activation = ConsistentRetailActivation(),
                UnavailableSources = new[]
                {
                    "Registry: UnauthorizedAccessException",
                    "ScheduledTask: query unavailable",
                    "Service: ManagementException"
                }
            });
            Assert.AreEqual(
                CrackTraceStatus.Unknown,
                result.Checks.Single(x => x.Order == 6).Status);
            Assert.AreEqual(
                CrackTraceStatus.Unknown,
                result.Checks.Single(x => x.Order == 7).Status);
            Assert.AreEqual(CrackTraceVerdict.Inconclusive, result.Verdict);
        }

        private static Task<CrackTraceAnalysisResult> Analyze(
            CrackTraceEvidenceSnapshot snapshot)
        {
            return new WindowsCrackTraceAnalyzer(new FakeSource(snapshot))
                .AnalyzeAsync(new SystemContext { OsName = "Windows" }, CancellationToken.None);
        }

        private static WindowsActivationTrace ConsistentRetailActivation()
        {
            return new WindowsActivationTrace
            {
                ProductName = "Windows 11 Pro",
                Channel = "Retail",
                ActivationId = "present",
                PartialProductKey = "XXXXX-XXXXX-XXXXX-XXXXX-3V66T",
                LicenseStatus = 1,
                IsPermanent = true,
                OemFirmwareKeyPresent = true
            };
        }

        private sealed class FakeSource : ICrackTraceEvidenceSource
        {
            private readonly CrackTraceEvidenceSnapshot _snapshot;
            public FakeSource(CrackTraceEvidenceSnapshot snapshot) { _snapshot = snapshot; }
            public Task<CrackTraceEvidenceSnapshot> CollectAsync(
                SystemContext context,
                CancellationToken cancellationToken) => Task.FromResult(_snapshot);
        }

        private sealed class ThrowingSource : ICrackTraceEvidenceSource
        {
            public Task<CrackTraceEvidenceSnapshot> CollectAsync(
                SystemContext context,
                CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("fixture failure");
            }
        }
    }
}
