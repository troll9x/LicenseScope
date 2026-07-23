namespace LicenseScope.Core.Models
{
    /// <summary>Represents a license conclusion without conflating unknown states with unlicensed.</summary>
    public enum LicenseStatus
    {
        Licensed,
        Unlicensed,
        Trial,
        GracePeriod,
        Expired,
        NeedsSignIn,
        NeedsOnlineVerification,
        NotInstalled,
        Unsupported,
        Unknown,
        Error
    }
}
