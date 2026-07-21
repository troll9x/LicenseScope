using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinLic.Scanners.Windows.Parsing;

namespace WinLic.Scanners.Windows.Tests
{
    [TestClass]
    public sealed class SlmgrParserTests
    {
        [DataTestMethod]
        [DataRow("The machine is permanently activated.")]
        [DataRow("Máy đã được kích hoạt vĩnh viễn.")]
        public void ParsesPermanentActivation(string fixture)
        {
            var result = new SlmgrXprParser().Parse(fixture, CultureInfo.InvariantCulture);
            Assert.IsTrue(result.Parsed);
            Assert.IsTrue(result.IsPermanent);
        }

        [DataTestMethod]
        [DataRow("Windows is in Notification mode.")]
        [DataRow("Windows đang ở chế độ thông báo.")]
        public void ParsesNotification(string fixture) => Assert.IsTrue(new SlmgrXprParser().Parse(fixture, CultureInfo.InvariantCulture).IndicatesUnlicensed);

        [TestMethod]
        public void ParsesEnglishExpiration() => Assert.IsNotNull(new SlmgrXprParser().Parse("Activation expiration: 12/31/2026 10:00:00 AM", new CultureInfo("en-US")).ExpirationDate);

        [TestMethod]
        public void ParsesVietnameseExpiration() => Assert.IsNotNull(new SlmgrXprParser().Parse("Hết hạn kích hoạt: 31/12/2026 10:00:00", new CultureInfo("vi-VN")).ExpirationDate);

        [DataTestMethod]
        [DataRow("")]
        [DataRow("言語を解析できません")]
        [DataRow("Malformed: not-a-date")]
        public void UnknownOutputRemainsUnknown(string fixture) => Assert.IsFalse(new SlmgrXprParser().Parse(fixture, CultureInfo.InvariantCulture).Parsed);

        [TestMethod]
        public void HandlesCrLfAndLf()
        {
            Assert.IsTrue(new SlmgrXprParser().Parse("Header\r\nThe machine is permanently activated.", CultureInfo.InvariantCulture).Parsed);
            Assert.IsTrue(new SlmgrXprParser().Parse("Header\nThe machine is permanently activated.", CultureInfo.InvariantCulture).Parsed);
        }

        [TestMethod]
        public void ParsesEnglishDlvFixture()
        {
            var result = new SlmgrDlvParser().Parse("Description: Windows(R), RETAIL channel\r\nLicense Status: Licensed\r\nPartial Product Key: 3V66T", CultureInfo.InvariantCulture);
            Assert.AreEqual("Windows(R), RETAIL channel", result.Description);
            Assert.AreEqual("Licensed", result.LicenseStatusText);
            Assert.AreEqual("3V66T", result.PartialProductKey);
        }

        [TestMethod]
        public void ParsesVietnameseDlvFixture()
        {
            var result = new SlmgrDlvParser().Parse("Mô tả: Windows(R), RETAIL channel\nTrạng thái bản quyền: Đã cấp phép\nKhóa sản phẩm một phần: 3V66T", new CultureInfo("vi-VN"));
            Assert.IsTrue(result.Parsed);
            Assert.AreEqual("3V66T", result.PartialProductKey);
        }
    }
}
