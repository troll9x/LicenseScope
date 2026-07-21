using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinLic.Scanners.Windows.Acquisition;
using WinLic.Scanners.Windows.Classification;
using WinLic.Scanners.Windows.Models;

namespace WinLic.Scanners.Windows.Tests
{
    [TestClass]
    public sealed class WindowsProductSelectorTests
    {
        [TestMethod]
        public void SelectsSingleWindowsRecord() => Assert.AreEqual("Windows Pro", Select(Record("Windows Pro", 1, "AAAAA")).Product?.Name);

        [TestMethod]
        public void ExcludesOfficeRecord()
        {
            var selected = new WindowsProductSelector().Select(new[] { Record("Office 2021", 1, "OFFIC", "office-app"), Record("Windows Pro", 0, "WIN00") });
            Assert.AreEqual("Windows Pro", selected.Product?.Name);
        }

        [TestMethod]
        public void LicensedRecordOutranksUnlicensed() => Assert.AreEqual((uint)1, Select(Record("Windows A", 0, "AAAAA"), Record("Windows B", 1, "BBBBB")).Product?.LicenseStatus);

        [TestMethod]
        public void GraceRecordOutranksUnlicensed() => Assert.AreEqual((uint)2, Select(Record("Windows A", 0, "AAAAA"), Record("Windows B", 2, "BBBBB")).Product?.LicenseStatus);

        [TestMethod]
        public void RecordWithPartialKeyOutranksMissingKey() => Assert.AreEqual("Windows B", Select(Record("Windows A", 1, ""), Record("Windows B", 1, "BBBBB")).Product?.Name);

        [TestMethod]
        public void MultipleEqualRecordsAreAmbiguous() => Assert.IsTrue(Select(Record("Windows A", 1, "AAAAA"), Record("Windows B", 1, "BBBBB")).Ambiguous);

        [TestMethod]
        public void NoRecordsReturnsNoProduct() => Assert.IsNull(new WindowsProductSelector().Select(new WindowsLicenseProductRecord[0]).Product);

        [TestMethod]
        public void MissingDescriptionDoesNotCrash() => Assert.IsNotNull(Select(Record("Windows Pro", 1, "AAAAA", description: "")).Product);

        private static WindowsProductSelection Select(params WindowsLicenseProductRecord[] records) => new WindowsProductSelector().Select(records);
        private static WindowsLicenseProductRecord Record(string name, uint status, string key, string appId = WindowsEvidenceCollector.WindowsApplicationId, string description = "RETAIL channel") => new WindowsLicenseProductRecord { Name = name, ApplicationId = appId, LicenseStatus = status, PartialProductKey = key, Description = description };
    }
}
