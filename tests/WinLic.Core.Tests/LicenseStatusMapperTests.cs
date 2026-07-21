using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinLic.Core.Models;
using WinLic.Core.Services;

namespace WinLic.Core.Tests
{
    [TestClass]
    public sealed class LicenseStatusMapperTests
    {
        [DataTestMethod]
        [DataRow(LicenseStatus.Licensed, true)]
        [DataRow(LicenseStatus.Unlicensed, false)]
        [DataRow(LicenseStatus.Expired, false)]
        public void MapsConclusiveStatuses(LicenseStatus status, bool expected)
        {
            Assert.AreEqual(expected, LicenseStatusMapper.ToIsLicensed(status));
        }

        [DataTestMethod]
        [DataRow(LicenseStatus.Trial)]
        [DataRow(LicenseStatus.GracePeriod)]
        [DataRow(LicenseStatus.NeedsSignIn)]
        [DataRow(LicenseStatus.NeedsOnlineVerification)]
        [DataRow(LicenseStatus.NotInstalled)]
        [DataRow(LicenseStatus.Unsupported)]
        [DataRow(LicenseStatus.Unknown)]
        [DataRow(LicenseStatus.Error)]
        public void MapsNonConclusiveStatusesToNull(LicenseStatus status)
        {
            Assert.IsNull(LicenseStatusMapper.ToIsLicensed(status));
        }
    }
}
