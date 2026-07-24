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
        public async Task LicenseStatusOneAloneNeverProvesCleanProvenance()
        {
            var result = await Analyze(new CrackTraceEvidenceSnapshot
            {
                Activation = ConsistentRetailActivation()
            });
            Assert.AreEqual(WindowsActivationState.Activated, result.ActivationState);
            Assert.AreEqual(CrackTraceVerdict.TraceNotFound, result.TraceVerdict);
            Assert.IsTrue(result.ScanCompleted);
            Assert.IsTrue(result.ActivationDetected);
            Assert.IsFalse(result.TraceDetected);
            Assert.IsFalse(result.ProvenanceVerified);
            Assert.AreEqual(
                LicenseProvenanceVerdict.Unverified,
                result.ProvenanceVerdict);
            Assert.IsFalse(result.VerdictSummary.Contains("AN TOÀN"));
            Assert.IsFalse(result.Checks.Any(x =>
                x.Status == CrackTraceStatus.Detected ||
                x.Status == CrackTraceStatus.Suspicious));
        }

        [TestMethod]
        public async Task OneAllowlistedTaskNameProducesBinaryTraceDetected()
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
            Assert.AreEqual(CrackTraceVerdict.TraceDetected, result.TraceVerdict);
            Assert.IsTrue(result.TraceDetected);
            Assert.IsTrue(result.Evidence.Any(x => x.StartsWith("scheduled-tasks |")));
            Assert.AreEqual(
                CrackTraceStatus.Suspicious,
                result.Checks.Single(x => x.Order == 6).Status);
        }

        [TestMethod]
        public async Task TwoIndependentStrongSignalsProduceTraceDetected()
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
            Assert.AreEqual(CrackTraceVerdict.TraceDetected, result.TraceVerdict);
            Assert.IsTrue(result.TraceDetected);
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
            Assert.AreEqual(CrackTraceStatus.Suspicious, registry.Status);
            Assert.IsFalse(registry.IsStrongSignal);
            Assert.AreEqual(CrackTraceVerdict.TraceDetected, result.TraceVerdict);
            Assert.IsTrue(result.TraceDetected);
            Assert.IsTrue(result.Evidence.Any(x =>
                x.StartsWith("registry-interference |")));
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
                CrackTraceStatus.TraceNotFound,
                result.Checks.Single(x => x.Order == 1).Status);
            Assert.IsFalse(result.TraceDetected);
        }

        [TestMethod]
        public async Task DigitalEntitlementIsNotAutomaticallyMas()
        {
            var result = await Analyze(new CrackTraceEvidenceSnapshot
            {
                Activation = ConsistentRetailActivation()
            });
            var mas = result.Checks.Single(x => x.Order == 2);
            Assert.AreEqual(CrackTraceStatus.TraceNotFound, mas.Status);
            StringAssert.Contains(mas.Summary, "không chứng minh nguồn gốc");
            Assert.AreEqual(CrackTraceVerdict.TraceNotFound, result.TraceVerdict);
            Assert.IsFalse(result.TraceDetected);
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
        public async Task SourceFailureReturnsBinaryIncompleteScan()
        {
            var analyzer = new WindowsCrackTraceAnalyzer(new ThrowingSource());
            var result = await analyzer.AnalyzeAsync(
                new SystemContext { OsName = "Windows" },
                CancellationToken.None);
            Assert.AreEqual(7, result.Checks.Count);
            Assert.IsTrue(result.Checks.All(x => x.Status == CrackTraceStatus.Error));
            Assert.AreEqual(CrackTraceVerdict.TraceNotFound, result.TraceVerdict);
            Assert.IsFalse(result.ScanCompleted);
            Assert.IsFalse(result.TraceDetected);
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
            Assert.AreEqual(CrackTraceVerdict.TraceNotFound, result.TraceVerdict);
            Assert.IsFalse(result.ScanCompleted);
            Assert.IsFalse(result.TraceDetected);
            Assert.IsFalse(result.VerdictSummary.Contains("AN TOÀN"));
        }

        [TestMethod]
        public async Task NoArtifactsReturnsTraceNotFoundNotSafetyClaim()
        {
            var result = await Analyze(new CrackTraceEvidenceSnapshot());
            Assert.AreEqual(
                CrackTraceVerdict.TraceNotFound,
                result.TraceVerdict);
            StringAssert.StartsWith(
                result.VerdictSummary,
                "KHÔNG PHÁT HIỆN DẤU VẾT:");
            Assert.IsFalse(result.VerdictSummary.Contains("AN TOÀN"));
            Assert.IsFalse(result.VerdictSummary.Contains("Bản quyền hợp lệ"));
            Assert.IsFalse(result.VerdictSummary.Contains("Không sử dụng crack"));
        }

        [TestMethod]
        public async Task OemDmAndMatchingFirmwareAreConsistentStateOnly()
        {
            var activation = ConsistentRetailActivation();
            activation.Channel = "OEM_DM";
            activation.ProductName = "Windows 11 Pro";
            activation.FirmwareEdition = "Windows 11 Professional OEM_DM";
            var result = await Analyze(
                new CrackTraceEvidenceSnapshot { Activation = activation });
            Assert.AreEqual(
                LicenseProvenanceVerdict.ConsistentState,
                result.ProvenanceVerdict);
            Assert.AreEqual(CrackTraceVerdict.TraceNotFound, result.TraceVerdict);
            Assert.IsTrue(result.ActivationDetected);
            Assert.IsFalse(result.TraceDetected);
            Assert.IsFalse(result.ProvenanceVerified);
            Assert.AreNotEqual(
                "VERIFIED",
                CrackTraceVerdictNames.ToMachineValue(result.ProvenanceVerdict));
        }

        [TestMethod]
        public async Task HwidLikeDigitalActivationWithoutArtifactsReportsNoTraceMatch()
        {
            var activation = ConsistentRetailActivation();
            activation.OemFirmwareKeyPresent = false;
            var result = await Analyze(
                new CrackTraceEvidenceSnapshot { Activation = activation });
            Assert.AreEqual(WindowsActivationState.Activated, result.ActivationState);
            Assert.AreEqual(CrackTraceVerdict.TraceNotFound, result.TraceVerdict);
            Assert.IsTrue(result.ActivationDetected);
            Assert.IsFalse(result.TraceDetected);
            Assert.IsFalse(result.ProvenanceVerified);
            StringAssert.StartsWith(
                result.VerdictSummary,
                "KHÔNG PHÁT HIỆN DẤU VẾT:");
        }

        [TestMethod]
        public async Task DeepScanDisabledDoesNotInspectForensicSources()
        {
            var source = new FakeSource(new CrackTraceEvidenceSnapshot());
            var analyzer = new WindowsCrackTraceAnalyzer(source);
            var result = await analyzer.AnalyzeAsync(
                new SystemContext { OsName = "Windows" },
                CancellationToken.None);
            Assert.AreEqual(0, source.DeepCalls);
            Assert.IsFalse(result.DeepForensicScanEnabled);
            Assert.AreEqual(
                DetectionCoverageStatus.NotChecked,
                result.DetectionCoverage.Single(x =>
                    x.Id == "historical-execution-traces").Status);
        }

        [TestMethod]
        public async Task DeepScanRequiresConsentAndReportsUnavailableAsUnknown()
        {
            var source = new FakeSource(
                new CrackTraceEvidenceSnapshot(),
                new CrackTraceEvidenceSnapshot
                {
                    DeepForensicScanPerformed = true,
                    UnavailableSources = new[] { "Prefetch: UnauthorizedAccessException" }
                });
            var analyzer = new WindowsCrackTraceAnalyzer(source);
            var withoutConsent = await analyzer.AnalyzeAsync(
                new SystemContext { OsName = "Windows" },
                new CrackTraceScanOptions { DeepForensicScan = true },
                CancellationToken.None);
            Assert.AreEqual(CrackTraceVerdict.TraceNotFound, withoutConsent.TraceVerdict);
            Assert.IsFalse(withoutConsent.ScanCompleted);
            Assert.IsFalse(withoutConsent.TraceDetected);
            Assert.AreEqual(0, source.DeepCalls);

            var withConsent = await analyzer.AnalyzeAsync(
                new SystemContext { OsName = "Windows" },
                new CrackTraceScanOptions
                {
                    DeepForensicScan = true,
                    UserConsented = true
                },
                CancellationToken.None);
            Assert.AreEqual(1, source.DeepCalls);
            Assert.AreEqual(
                DetectionCoverageStatus.Unknown,
                withConsent.DetectionCoverage.Single(x =>
                    x.Id == "historical-execution-traces").Status);
            Assert.AreEqual(CrackTraceVerdict.TraceNotFound, withConsent.TraceVerdict);
            Assert.IsFalse(withConsent.ScanCompleted);
            Assert.IsFalse(withConsent.TraceDetected);
        }

        [TestMethod]
        public async Task StructuredFieldsAreAlwaysPopulated()
        {
            var result = await Analyze(new CrackTraceEvidenceSnapshot());
            Assert.AreEqual(5, result.DetectionCoverage.Count);
            Assert.AreEqual(
                DetectionCoverageStatus.Checked,
                result.DetectionCoverage.Single(x =>
                    x.Id == "current-licensing-state").Status);
            Assert.IsTrue(result.DetectionCoverage.Single(x =>
                x.Id == "current-licensing-state").Checked);
            Assert.AreEqual(
                DetectionCoverageStatus.Checked,
                result.DetectionCoverage.Single(x =>
                    x.Id == "current-kms-configuration").Status);
            Assert.AreEqual(
                DetectionCoverageStatus.Checked,
                result.DetectionCoverage.Single(x =>
                    x.Id == "known-services-tasks-files").Status);
            Assert.AreEqual(
                DetectionCoverageStatus.NotChecked,
                result.DetectionCoverage.Single(x =>
                    x.Id == "historical-execution-traces").Status);
            Assert.AreEqual(
                DetectionCoverageStatus.NotTechnicallyVerifiable,
                result.DetectionCoverage.Single(x =>
                    x.Id == "digital-license-provenance").Status);
            Assert.IsFalse(result.DetectionCoverage.Single(x =>
                x.Id == "digital-license-provenance").Checked);
            Assert.IsTrue(result.BlindSpots.Count > 0);
            Assert.AreEqual(0, result.Evidence.Count);
            Assert.IsTrue(result.Confidence >= 0);
            Assert.IsTrue(result.ScanCompleted);
            Assert.IsFalse(result.ActivationDetected);
            Assert.IsFalse(result.TraceDetected);
            Assert.IsFalse(result.ProvenanceVerified);
            Assert.AreEqual(
                "TRACE_NOT_DETECTED",
                CrackTraceVerdictNames.ToMachineValue(result.TraceVerdict));
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

        private sealed class FakeSource :
            ICrackTraceEvidenceSource,
            IDeepCrackTraceEvidenceSource
        {
            private readonly CrackTraceEvidenceSnapshot _snapshot;
            private readonly CrackTraceEvidenceSnapshot _deepSnapshot;
            public int DeepCalls { get; private set; }
            public FakeSource(
                CrackTraceEvidenceSnapshot snapshot,
                CrackTraceEvidenceSnapshot? deepSnapshot = null)
            {
                _snapshot = snapshot;
                _deepSnapshot = deepSnapshot ?? new CrackTraceEvidenceSnapshot
                {
                    DeepForensicScanPerformed = true
                };
            }
            public Task<CrackTraceEvidenceSnapshot> CollectAsync(
                SystemContext context,
                CancellationToken cancellationToken) => Task.FromResult(_snapshot);
            public Task<CrackTraceEvidenceSnapshot> CollectDeepForensicAsync(
                SystemContext context,
                CancellationToken cancellationToken)
            {
                DeepCalls++;
                return Task.FromResult(_deepSnapshot);
            }
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
