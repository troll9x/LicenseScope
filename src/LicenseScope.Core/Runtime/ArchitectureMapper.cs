using System;
using LicenseScope.Core.Models;

namespace LicenseScope.Core.Runtime
{
    /// <summary>Maps Windows architecture labels without requiring newer RuntimeInformation APIs.</summary>
    public static class ArchitectureMapper
    {
        public static OperatingSystemArchitecture MapOperatingSystem(string? architecture, bool is64BitOperatingSystem)
        {
            switch ((architecture ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "X86": return OperatingSystemArchitecture.X86;
                case "AMD64":
                case "X64": return OperatingSystemArchitecture.X64;
                case "ARM": return OperatingSystemArchitecture.Arm32;
                case "ARM64": return OperatingSystemArchitecture.Arm64;
                default: return is64BitOperatingSystem ? OperatingSystemArchitecture.X64 : OperatingSystemArchitecture.Unknown;
            }
        }

        public static ProcessArchitecture MapProcess(string? architecture, bool is64BitProcess)
        {
            switch ((architecture ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "X86": return ProcessArchitecture.X86;
                case "AMD64":
                case "X64": return ProcessArchitecture.X64;
                case "ARM": return ProcessArchitecture.Arm32;
                case "ARM64": return ProcessArchitecture.Arm64;
                default: return is64BitProcess ? ProcessArchitecture.X64 : ProcessArchitecture.Unknown;
            }
        }
    }
}
