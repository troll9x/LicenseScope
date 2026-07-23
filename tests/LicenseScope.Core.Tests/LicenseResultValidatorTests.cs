using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LicenseScope.Core.Models;
using LicenseScope.Core.Services;

namespace LicenseScope.Core.Tests
{
    [TestClass]
    public sealed class LicenseResultValidatorTests
    {
        [TestMethod]
        public void ValidResultHasNoIssues()
        {
            var result = Valid(LicenseStatus.Licensed, true);
            Assert.AreEqual(0, LicenseResultValidator.NormalizeAndValidate(result).Count);
        }

        [DataTestMethod]
        [DataRow(LicenseStatus.Licensed, false)]
        [DataRow(LicenseStatus.Unlicensed, true)]
        [DataRow(LicenseStatus.NeedsOnlineVerification, false)]
        public void ContradictoryMappingIsRejected(LicenseStatus status, bool isLicensed)
        {
            Assert.IsTrue(LicenseResultValidator.NormalizeAndValidate(Valid(status, isLicensed)).Count > 0);
        }

        [TestMethod]
        public void NullCollectionsAreNormalized()
        {
            var result = Valid(LicenseStatus.Unknown, null);
            result.Evidence = null!;
            result.Warnings = null!;
            LicenseResultValidator.NormalizeAndValidate(result);
            Assert.IsNotNull(result.Evidence);
            Assert.IsNotNull(result.Warnings);
        }

        [TestMethod]
        public void NotInstalledLicensedProductIsRejected()
        {
            var result = Valid(LicenseStatus.Licensed, true);
            result.Installed = false;
            CollectionAssert.Contains(new List<string>(LicenseResultValidator.NormalizeAndValidate(result)), "A product cannot be Licensed when Installed is false.");
        }

        [TestMethod]
        public void RequiredIdentityFieldsAreValidated()
        {
            var result = Valid(LicenseStatus.Unknown, null);
            result.ScannerId = string.Empty;
            result.Vendor = string.Empty;
            result.ProductName = string.Empty;
            var issues = new List<string>(LicenseResultValidator.NormalizeAndValidate(result));
            CollectionAssert.Contains(issues, "ScannerId is required.");
            CollectionAssert.Contains(issues, "Vendor is required.");
            CollectionAssert.Contains(issues, "ProductName is required.");
        }

        private static LicenseResult Valid(LicenseStatus status, bool? isLicensed) => new LicenseResult
        {
            ScannerId = "test.scanner", Vendor = "Test Vendor", ProductName = "Test Product",
            Installed = true, Status = status, IsLicensed = isLicensed
        };
    }
}
