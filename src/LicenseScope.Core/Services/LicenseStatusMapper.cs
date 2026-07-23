using LicenseScope.Core.Models;

namespace LicenseScope.Core.Services
{
    /// <summary>Central mapping from a detailed status to its nullable licensed conclusion.</summary>
    public static class LicenseStatusMapper
    {
        public static bool? ToIsLicensed(LicenseStatus status)
        {
            switch (status)
            {
                case LicenseStatus.Licensed: return true;
                case LicenseStatus.Unlicensed:
                case LicenseStatus.Expired: return false;
                default: return null;
            }
        }
    }
}
