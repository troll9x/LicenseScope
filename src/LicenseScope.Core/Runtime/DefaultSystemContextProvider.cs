using System;
using System.Security.Principal;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;

namespace LicenseScope.Core.Runtime
{
    /// <summary>Creates a conservative context from APIs available on .NET Framework 4.8.</summary>
    public sealed class DefaultSystemContextProvider : ISystemContextProvider
    {
        public SystemContext GetCurrent()
        {
            var version = Environment.OSVersion.Version;
            var processArchitecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            var osArchitecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432") ?? processArchitecture;
            return new SystemContext
            {
                MachineName = Environment.MachineName,
                OsName = Environment.OSVersion.Platform.ToString(),
                OsVersion = version.ToString(),
                OsBuild = version.Build.ToString(),
                OsArchitecture = ArchitectureMapper.MapOperatingSystem(osArchitecture, Environment.Is64BitOperatingSystem),
                ProcessArchitecture = ArchitectureMapper.MapProcess(processArchitecture, Environment.Is64BitProcess),
                Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                Is64BitProcess = Environment.Is64BitProcess,
                IsAdministrator = IsAdministrator(),
                WindowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                ProgramFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                ProgramFilesX86Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };
        }

        private static bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
