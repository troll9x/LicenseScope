using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinLic.Core.Contracts;
using WinLic.Core.Models;
using WinLic.Scanners.Windows.Classification;
using WinLic.Scanners.Windows.Models;

namespace WinLic.Scanners.Windows.Tests
{
    [TestClass]
    public sealed class WindowsClassificationTests
    {
        [DataTestMethod]
        [DataRow(0u, LicenseStatus.Unlicensed)]
        [DataRow(1u, LicenseStatus.Licensed)]
        [DataRow(2u, LicenseStatus.GracePeriod)]
        [DataRow(3u, LicenseStatus.GracePeriod)]
        [DataRow(4u, LicenseStatus.GracePeriod)]
        [DataRow(5u, LicenseStatus.Unknown)]
        [DataRow(6u, LicenseStatus.GracePeriod)]
        [DataRow(99u, LicenseStatus.Unknown)]
        public void MapsEveryWmiStatus(uint value, LicenseStatus expected) => Assert.AreEqual(expected, WindowsLicenseClassifier.MapStatus(value, new SlmgrXprParseResult(), new List<string>()));

        [TestMethod]
        public void NotificationWithUnlicensedXprIsUnlicensed() => Assert.AreEqual(LicenseStatus.Unlicensed, WindowsLicenseClassifier.MapStatus(5, new SlmgrXprParseResult { IndicatesUnlicensed = true }, new List<string>()));

        [DataTestMethod]
        [DataRow("RETAIL channel", "", "Retail")]
        [DataRow("OEM_DM channel", "", "OEM_DM")]
        [DataRow("oem_slp channel", "", "OEM_SLP")]
        [DataRow("OEM_COA", "", "OEM_COA")]
        [DataRow("VOLUME_MAK channel", "", "Volume_MAK")]
        [DataRow("VOLUME_KMSCLIENT channel", "", "Volume_KMSCLIENT")]
        [DataRow("Evaluation", "", "Evaluation")]
        [DataRow(null, null, "Unknown")]
        public void ClassifiesChannels(string? description, string? channel, string expected) => Assert.AreEqual(expected, new WindowsChannelClassifier().Classify(description ?? string.Empty, channel ?? string.Empty));

        [TestMethod]
        public void InfersDigitalLicenseOnlyWithCorroboration()
        {
            var value = Classify(Product(1, "RETAIL", "3V66T"), new WindowsLicenseEvidence { Xpr = new SlmgrXprParseResult { Parsed = true, IsPermanent = true } });
            Assert.AreEqual("DigitalLicense", value.ActivationMethod);
            Assert.AreEqual(ConfidenceLevel.High, value.Confidence);
        }

        [TestMethod]
        public void GenericKeyAloneDoesNotInferDigitalLicense() => Assert.AreEqual("Retail product key", Classify(Product(1, "RETAIL", "3V66T"), new WindowsLicenseEvidence()).ActivationMethod);

        [TestMethod]
        public void KmsExcludesDigitalInference() => Assert.AreEqual("KMS client", Classify(Product(1, "VOLUME_KMSCLIENT", "3V66T"), new WindowsLicenseEvidence { Xpr = new SlmgrXprParseResult { Parsed = true, IsPermanent = true } }).ActivationMethod);

        [TestMethod]
        public void LicensedKmsIsLegitimateAndWarnsAboutRenewal()
        {
            var value = Classify(Product(1, "VOLUME_KMSCLIENT", "T83GX"), new WindowsLicenseEvidence());
            Assert.AreEqual(LicenseStatus.Licensed, value.Status);
            StringAssert.Contains(string.Join(" ", value.Warnings), "legitimate");
        }

        [TestMethod]
        public void ExpiredKmsDateMapsExpired()
        {
            var value = Classify(Product(1, "VOLUME_KMSCLIENT", "T83GX"), new WindowsLicenseEvidence { Xpr = new SlmgrXprParseResult { Parsed = true, ExpirationDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) } });
            Assert.AreEqual(LicenseStatus.Expired, value.Status);
        }

        [TestMethod]
        public void OemKeyDoesNotOverrideUnlicensedStatus()
        {
            var value = Classify(Product(0, "OEM_DM", "ABCDE"), new WindowsLicenseEvidence { MaskedOa3Key = "XXXXX-XXXXX-XXXXX-XXXXX-ABCDE" });
            Assert.AreEqual(LicenseStatus.Unlicensed, value.Status);
        }

        private static WindowsActivationClassification Classify(WindowsLicenseProductRecord product, WindowsLicenseEvidence evidence) => new WindowsLicenseClassifier(new WindowsChannelClassifier(), new WindowsKnownKeyCatalog(), new FixedClock()).Classify(product, evidence, false);
        private static WindowsLicenseProductRecord Product(uint status, string description, string key) => new WindowsLicenseProductRecord { Name = "Windows", LicenseStatus = status, Description = description, PartialProductKey = key };
        private sealed class FixedClock : ISystemClock { public DateTimeOffset UtcNow => new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero); }
    }
}
