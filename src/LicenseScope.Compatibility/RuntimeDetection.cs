using System;
using Microsoft.Win32;

namespace LicenseScope.Compatibility
{
    public static class NetFrameworkReleaseMapper
    {
        public static Version Map(int? release)
        {
            if (!release.HasValue) return new Version(0, 0);
            if (release.Value >= 533320) return new Version(4, 8, 1);
            if (release.Value >= 528040) return new Version(4, 8, 0);
            return new Version(0, 0);
        }
    }

    public sealed class RuntimeEnvironmentDetector
    {
        private readonly IArchitectureProbe _architecture;
        public RuntimeEnvironmentDetector(IArchitectureProbe architecture) { _architecture = architecture; }

        public RuntimeEnvironmentInfo Detect()
        {
            var architecture = _architecture.ProbeCurrentProcess();
            int? release = null;
            string product = string.Empty, build = Environment.OSVersion.Version.Build.ToString();
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                        if (key?.GetValue("Release") is int value) { release = value; break; }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException) { }
            }
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    { product = key?.GetValue("ProductName") as string ?? product; build = key?.GetValue("CurrentBuildNumber") as string ?? build; }
                    if (!string.IsNullOrWhiteSpace(product)) break;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException) { }
            }
            var reportedVersion = Environment.OSVersion.Version; int parsedBuild;
            if (int.TryParse(build, out parsedBuild) && parsedBuild >= 10240) reportedVersion = new Version(10, 0, parsedBuild);
            return new RuntimeEnvironmentInfo { WindowsProductName = product, WindowsVersion = reportedVersion, WindowsBuild = build, NativeOsArchitecture = architecture.NativeArchitecture, ProcessArchitecture = architecture.ProcessArchitecture, ExecutionMode = architecture.ExecutionMode, Is64BitOperatingSystem = Environment.Is64BitOperatingSystem, Is64BitProcess = Environment.Is64BitProcess, IsEmulated = architecture.IsEmulated, NetFrameworkReleaseKey = release, InstalledNetFrameworkVersion = NetFrameworkReleaseMapper.Map(release), FrameworkDetectionSource = release.HasValue ? "HKLM .NET Framework Release" : "Unavailable", IsWindowsServer = product.IndexOf("Server", StringComparison.OrdinalIgnoreCase) >= 0 };
        }
    }
}
