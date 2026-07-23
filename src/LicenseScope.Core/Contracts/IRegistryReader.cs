namespace LicenseScope.Core.Contracts
{
    /// <summary>Read-only abstraction for future platform-specific registry evidence providers.</summary>
    public interface IRegistryReader
    {
        object? GetLocalMachineValue(string subKeyPath, string valueName, RegistryViewPreference view);
    }

    public enum RegistryViewPreference { Default, Registry32, Registry64 }
}
