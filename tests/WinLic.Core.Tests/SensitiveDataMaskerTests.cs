using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinLic.Core.Security;

namespace WinLic.Core.Tests
{
    [TestClass]
    public sealed class SensitiveDataMaskerTests
    {
        [TestMethod]
        public void MasksValidProductKey() => Assert.AreEqual("XXXXX-XXXXX-XXXXX-XXXXX-EEEEE", SensitiveDataMasker.MaskProductKey("AAAAA-BBBBB-CCCCC-DDDDD-EEEEE"));

        [TestMethod]
        public void NormalizesLowercaseProductKeySuffix() => Assert.AreEqual("XXXXX-XXXXX-XXXXX-XXXXX-EEEEE", SensitiveDataMasker.MaskProductKey("aaaaa-bbbbb-ccccc-ddddd-eeeee"));

        [TestMethod]
        public void ProductKeyWithWhitespaceIsNotAccepted() => Assert.AreEqual(" AAAAA-BBBBB-CCCCC-DDDDD-EEEEE ", SensitiveDataMasker.MaskProductKey(" AAAAA-BBBBB-CCCCC-DDDDD-EEEEE "));

        [DataTestMethod]
        [DataRow(null, "")]
        [DataRow("", "")]
        [DataRow("SHORT", "SHORT")]
        [DataRow("not-a-product-key", "not-a-product-key")]
        public void ProductKeyInvalidInputsAreSafe(string? input, string expected) => Assert.AreEqual(expected, SensitiveDataMasker.MaskProductKey(input));

        [TestMethod]
        public void MasksEmail() => Assert.AreEqual("n********a@example.com", SensitiveDataMasker.MaskEmail("nguyenvana@example.com"));

        [TestMethod]
        public void MasksSingleCharacterEmailLocalPart() => Assert.AreEqual("*@example.com", SensitiveDataMasker.MaskEmail("a@example.com"));

        [DataTestMethod]
        [DataRow(null, "")]
        [DataRow("", "")]
        [DataRow("unusual", "unusual")]
        [DataRow("a@@example.com", "a@@example.com")]
        public void UnusualEmailsAreSafe(string? input, string expected) => Assert.AreEqual(expected, SensitiveDataMasker.MaskEmail(input));

        [TestMethod]
        public void MasksUsernameInWindowsPath() => Assert.AreEqual(@"C:\Users\<USER>\AppData\Local\Adobe", SensitiveDataMasker.MaskWindowsPath(@"C:\Users\NguyenVanA\AppData\Local\Adobe"));

        [TestMethod]
        public void MasksUsernameOnOtherDriveAndMixedCase() => Assert.AreEqual(@"D:\uSeRs\<USER>\Data", SensitiveDataMasker.MaskWindowsPath(@"D:\uSeRs\PrivateName\Data"));

        [TestMethod]
        public void LeavesNonUserPathUnchanged() => Assert.AreEqual(@"C:\Program Files\Adobe", SensitiveDataMasker.MaskWindowsPath(@"C:\Program Files\Adobe"));

        [TestMethod]
        public void MachineNameIsDeterministicallyAnonymized()
        {
            var masked = SensitiveDataMasker.AnonymizeMachineName("DESKTOP-PRIVATE");
            Assert.AreEqual(masked, SensitiveDataMasker.AnonymizeMachineName("desktop-private"));
            Assert.IsFalse(masked.Contains("PRIVATE"));
            StringAssert.StartsWith(masked, "MACHINE-");
            Assert.AreNotEqual(masked, SensitiveDataMasker.AnonymizeMachineName("OTHER-MACHINE"));
        }

        [DataTestMethod]
        [DataRow("access_token")]
        [DataRow("Password")]
        [DataRow("sessionCookie")]
        [DataRow("AuthorizationHeader")]
        public void RedactsGenericSecretFields(string name) => Assert.AreEqual("[REDACTED]", SensitiveDataMasker.MaskNamedValue(name, "do-not-output"));
    }
}
