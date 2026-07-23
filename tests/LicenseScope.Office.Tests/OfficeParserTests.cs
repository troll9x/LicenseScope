using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LicenseScope.Office.Models;
using LicenseScope.Office.Parsing;

namespace LicenseScope.Office.Tests
{
    [TestClass] public sealed class OfficeParserTests
    {
        [TestMethod]
        [DataRow("LICENSE NAME: Office19ProPlus2019VL_KMS_Client_AE\nLICENSE DESCRIPTION: Office19, VOLUME_KMSCLIENT channel\nLICENSE STATUS:  ---LICENSED---\nLast 5 characters of installed product key: ABCDE", OfficeProductFamily.OfficeSuite, "ABCDE")]
        [DataRow("TÊN GIẤY PHÉP: ProjectPro2021VL_MAK_AE\r\nMÔ TẢ GIẤY PHÉP: VOLUME_MAK channel\r\nTRẠNG THÁI GIẤY PHÉP: ĐÃ CẤP PHÉP\r\n5 ký tự cuối: V1234", OfficeProductFamily.Project, "V1234")]
        [DataRow("LICENSE NAME: VisioPro2021Retail\nLICENSE STATUS: UNLICENSED", OfficeProductFamily.Visio, "")]
        public void OsppParsesProductBlocks(string fixture, OfficeProductFamily family, string key)
        { var value = new OsppStatusParser().Parse(fixture, CultureInfo.InvariantCulture); Assert.AreEqual(1, value.Count); Assert.AreEqual(family, value[0].Family); Assert.AreEqual(key, value[0].PartialProductKey); }

        [TestMethod] public void OsppKeepsMultipleProductsSeparate()
        { var value = new OsppStatusParser().Parse("LICENSE NAME: OfficeProRetail\nLICENSE STATUS: LICENSED\nLICENSE NAME: VisioProRetail\nLICENSE STATUS: NOTIFICATIONS", CultureInfo.InvariantCulture); Assert.AreEqual(2, value.Count); Assert.AreEqual(OfficeProductFamily.Visio, value[1].Family); }
        [TestMethod] public void OsppUnknownAndEmptyAreSafe() { Assert.AreEqual(0, new OsppStatusParser().Parse("langue inattendue", CultureInfo.InvariantCulture).Count); Assert.AreEqual(0, new OsppStatusParser().Parse(string.Empty, CultureInfo.InvariantCulture).Count); }
        [TestMethod] public void OsppDuplicateBlocksRemainAvailableForCollectorDeduplication() { Assert.AreEqual(2, new OsppStatusParser().Parse("LICENSE NAME: OfficeRetail\nLICENSE STATUS: LICENSED\nLICENSE NAME: OfficeRetail\nLICENSE STATUS: LICENSED", CultureInfo.InvariantCulture).Count); }

        [TestMethod]
        [DataRow("Name: O365ProPlusRetail\nLicense Type: User|Subscription\nState: Licensed\nEmail: person@example.com", OfficeProductFamily.Microsoft365Apps, "p****n@example.com")]
        [DataRow("Tên sản phẩm: VisioProRetail\nLoại giấy phép: User|Subscription\nTrạng thái: Cần đăng nhập", OfficeProductFamily.Visio, "")]
        [DataRow("Name: ProjectProRetail\r\nType: Device|Subscription\r\nState: Expired", OfficeProductFamily.Project, "")]
        public void VNextParsesAndMasksAccounts(string fixture, OfficeProductFamily family, string account)
        { var value = new VNextStatusParser().Parse(fixture, CultureInfo.InvariantCulture); Assert.AreEqual(1, value.Count); Assert.AreEqual(family, value[0].Family); Assert.AreEqual(account, value[0].MaskedAccount); }
        [TestMethod] public void VNextParsesMultipleProducts() { Assert.AreEqual(2, new VNextStatusParser().Parse("Name: O365ProPlusRetail\nState: Licensed\nName: VisioProRetail\nState: Licensed", CultureInfo.InvariantCulture).Count); }
        [TestMethod] public void VNextMalformedIsSafe() { Assert.AreEqual(0, new VNextStatusParser().Parse("{broken output}", CultureInfo.InvariantCulture).Count); }
        [TestMethod] public void VNextParsesExpiration() { Assert.IsNotNull(new VNextStatusParser().Parse("Name: O365ProPlusRetail\nExpiration: 2030-01-02T03:04:00Z", CultureInfo.InvariantCulture)[0].ExpirationDate); }
    }
}
