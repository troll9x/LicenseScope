using Microsoft.VisualStudio.TestTools.UnitTesting;
using LicenseScope.Core.Models;
using LicenseScope.Office.Classification;
using LicenseScope.Office.Models;

namespace LicenseScope.Office.Tests
{
    [TestClass] public sealed class OfficeClassificationTests
    {
        [TestMethod]
        [DataRow("LICENSED", LicenseStatus.Licensed)] [DataRow("UNLICENSED", LicenseStatus.Unlicensed)] [DataRow("NOTIFICATION", LicenseStatus.GracePeriod)] [DataRow("OOB_GRACE", LicenseStatus.GracePeriod)] [DataRow("TRIAL", LicenseStatus.Trial)] [DataRow("EXPIRED", LicenseStatus.Expired)] [DataRow("Needs sign in", LicenseStatus.NeedsSignIn)] [DataRow("Online refresh required", LicenseStatus.NeedsOnlineVerification)] [DataRow("???", LicenseStatus.Unknown)]
        public void MapsStatesConservatively(string state, LicenseStatus expected) { Assert.AreEqual(expected, new OfficeLicenseClassifier().Status(new OfficeProductEvidence { LicenseState = state })); }
        [TestMethod] public void InstalledM365WithoutToolNeedsOnlineVerification() { Assert.AreEqual(LicenseStatus.NeedsOnlineVerification, new OfficeLicenseClassifier().Status(new OfficeProductEvidence { Family = OfficeProductFamily.Microsoft365Apps })); }
        [TestMethod]
        [DataRow("RETAIL channel", "Retail")] [DataRow("VOLUME_MAK channel", "Volume_MAK")] [DataRow("VOLUME_KMSCLIENT channel", "Volume_KMSCLIENT")] [DataRow("User|Subscription", "Subscription")] [DataRow("", "Unknown")]
        public void MapsChannels(string channel, string expected) { Assert.AreEqual(expected, new OfficeLicenseClassifier().LicenseType(new OfficeProductEvidence { Channel = channel })); }
        [TestMethod] public void KmsIsNotTreatedAsPiracy() { var p = new OfficeProductEvidence { Channel = "VOLUME_KMSCLIENT", LicenseState = "LICENSED" }; var c = new OfficeLicenseClassifier(); Assert.AreEqual(LicenseStatus.Licensed, c.Status(p)); Assert.AreEqual("Volume_KMSCLIENT", c.LicenseType(p)); }
        [TestMethod] [DataRow("ProjectPro2021", OfficeProductFamily.Project)] [DataRow("VisioPro2021", OfficeProductFamily.Visio)] [DataRow("O365ProPlusRetail", OfficeProductFamily.Microsoft365Apps)] [DataRow("OfficeProPlus", OfficeProductFamily.OfficeSuite)]
        public void ProductFamiliesAreSeparated(string id, OfficeProductFamily expected) { Assert.AreEqual(expected, Detection.OfficeInstallationDetector.FromId(id, "", "x64", "ClickToRun", "", true).Family); }
    }
}
