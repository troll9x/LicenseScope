using System;
using LicenseScope.Core.Models;
using LicenseScope.Office.Models;

namespace LicenseScope.Office.Classification
{
    public sealed class OfficeLicenseClassifier
    {
        public LicenseStatus Status(OfficeProductEvidence product)
        {
            var value = (product.LicenseState ?? string.Empty).ToLowerInvariant();
            if (value.Contains("unlicensed") || value.Contains("chưa cấp phép")) return LicenseStatus.Unlicensed;
            if (value.Contains("licensed") || value.Contains("đã cấp phép")) return LicenseStatus.Licensed;
            if (value.Contains("notification") || value.Contains("grace") || value.Contains("gia hạn")) return LicenseStatus.GracePeriod;
            if (value.Contains("trial") || value.Contains("evaluation") || value.Contains("dùng thử")) return LicenseStatus.Trial;
            if (value.Contains("expired") || value.Contains("hết hạn")) return LicenseStatus.Expired;
            if (value.Contains("sign") || value.Contains("đăng nhập")) return LicenseStatus.NeedsSignIn;
            if (value.Contains("refresh") || value.Contains("online")) return LicenseStatus.NeedsOnlineVerification;
            return product.Family == OfficeProductFamily.Microsoft365Apps && !product.FromOfficialTool ? LicenseStatus.NeedsOnlineVerification : LicenseStatus.Unknown;
        }

        public string LicenseType(OfficeProductEvidence product)
        {
            var value = ((product.Channel ?? string.Empty) + " " + (product.LicenseMode ?? string.Empty)).ToLowerInvariant();
            if (value.Contains("shared computer")) return "SharedComputerActivation";
            if (value.Contains("device")) return "DeviceBased";
            if (value.Contains("subscription") || product.Family == OfficeProductFamily.Microsoft365Apps) return "Subscription";
            if (value.Contains("kmsclient") || value.Contains("kms_client")) return "Volume_KMSCLIENT";
            if (value.Contains("mak")) return "Volume_MAK";
            if (value.Contains("retail")) return "Retail";
            if (value.Contains("trial") || value.Contains("evaluation")) return "Trial";
            return "Unknown";
        }
    }
}
