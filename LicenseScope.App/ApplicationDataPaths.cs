using System;
using System.IO;

namespace LicenseScope.App
{
    internal static class ApplicationDataPaths
    {
        public static string SettingsDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LicenseScope");

        public static void EnsureSettingsDirectory()
        {
            Directory.CreateDirectory(SettingsDirectory);
        }
    }
}
