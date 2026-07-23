namespace LicenseScope.Windows.Classification
{
    public interface IWindowsKnownKeyCatalog
    {
        bool IsGenericInstallationKey(string partialKey);
        bool IsVolumeClientKey(string partialKey);
        string GetDescription(string partialKey);
    }
}
