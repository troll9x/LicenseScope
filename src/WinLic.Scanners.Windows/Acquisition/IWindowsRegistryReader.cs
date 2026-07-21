using WinLic.Core.Contracts;

namespace WinLic.Scanners.Windows.Acquisition
{
    public interface IWindowsRegistryReader
    {
        RegistryReadResult ReadLocalMachineString(string subKeyPath, string valueName, RegistryViewPreference view);
    }
}
