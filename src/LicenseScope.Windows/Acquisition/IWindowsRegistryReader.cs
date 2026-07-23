using LicenseScope.Core.Contracts;

namespace LicenseScope.Windows.Acquisition
{
    public interface IWindowsRegistryReader
    {
        RegistryReadResult ReadLocalMachineString(string subKeyPath, string valueName, RegistryViewPreference view);
    }
}
