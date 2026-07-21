using System;
using System.Security;
using Microsoft.Win32;
using WinLic.Core.Contracts;

namespace WinLic.Scanners.Windows.Acquisition
{
    public sealed class WindowsRegistryReader : IWindowsRegistryReader
    {
        public RegistryReadResult ReadLocalMachineString(string subKeyPath, string valueName, RegistryViewPreference view)
        {
            try
            {
                var registryView = view == RegistryViewPreference.Registry32 ? RegistryView.Registry32 :
                    view == RegistryViewPreference.Registry64 ? RegistryView.Registry64 : RegistryView.Default;
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
                using (var key = baseKey.OpenSubKey(subKeyPath, writable: false))
                {
                    if (key == null) return new RegistryReadResult();
                    var value = key.GetValue(valueName) as string;
                    return new RegistryReadResult { Found = !string.IsNullOrWhiteSpace(value), Value = value ?? string.Empty };
                }
            }
            catch (UnauthorizedAccessException ex) { return Denied(ex); }
            catch (SecurityException ex) { return Denied(ex); }
            catch (Exception ex) when (ex is ArgumentException || ex is System.IO.IOException)
            {
                return new RegistryReadResult { ErrorMessage = ex.Message };
            }
        }

        private static RegistryReadResult Denied(Exception ex) => new RegistryReadResult { AccessDenied = true, ErrorMessage = ex.Message };
    }
}
