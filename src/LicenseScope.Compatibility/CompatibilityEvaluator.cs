using System;
using System.Collections.Generic;

namespace LicenseScope.Compatibility
{
    public sealed class CompatibilityEvaluator : ICompatibilityEvaluator
    {
        public CompatibilityAssessment Evaluate(RuntimeEnvironmentInfo environment, PayloadDescriptor payload)
        {
            var warnings = new List<string>(); var blocks = new List<string>();
            var version = environment.WindowsVersion;
            var isWin7Sp1 = version.Major == 6 && version.Minor == 1;
            var isWin8 = version.Major == 6 && version.Minor == 2;
            var isWin81 = version.Major == 6 && version.Minor == 3;
            var isWin10Or11 = version.Major == 10;
            var eol = isWin7Sp1 || isWin8 || isWin81 || isWin10Or11 && version.Build < 22000;
            if (isWin8) blocks.Add("Windows 8.0 is not a .NET Framework 4.8 target operating system.");
            else if (!isWin7Sp1 && !isWin81 && !isWin10Or11) blocks.Add("The Windows version is outside the current compatibility policy.");
            if (environment.IsWindowsServer) blocks.Add("Windows Server is not verified by the desktop compatibility policy.");
            if (environment.InstalledNetFrameworkVersion < payload.RequiredFrameworkVersion) blocks.Add("The required .NET Framework version is not detected.");
            if (payload.ProcessArchitecture == CpuArchitecture.X64 && environment.NativeOsArchitecture == CpuArchitecture.X86) blocks.Add("An x64 payload cannot run on x86 Windows.");
            if (payload.ProcessArchitecture == CpuArchitecture.X64 && environment.NativeOsArchitecture == CpuArchitecture.Arm64 && version.Build < 22000) blocks.Add("Windows 10 on Arm does not support the x64 payload.");
            if (payload.ProcessArchitecture == CpuArchitecture.Arm64 && (environment.NativeOsArchitecture != CpuArchitecture.Arm64 || version.Build < 22000 || payload.TargetFramework != "net481")) blocks.Add("Native ARM64 requires Windows 11 on Arm and .NET Framework 4.8.1.");
            if (eol) warnings.Add("This operating system is end of life and no longer receives standard security support.");
            if (environment.IsEmulated) warnings.Add("The current payload is running under architecture emulation.");
            var canRun = blocks.Count == 0;
            return new CompatibilityAssessment { CanStart = canRun, CanRunAudit = canRun, SupportLevel = canRun ? (eol ? CompatibilitySupportLevel.TechnicallyCompatible : payload.RuntimeVerified ? CompatibilitySupportLevel.Supported : CompatibilitySupportLevel.Experimental) : CompatibilitySupportLevel.Unsupported, IsEndOfLife = eol, RuntimeVerified = payload.RuntimeVerified, RecommendedPayloadId = Recommend(environment), Warnings = warnings, BlockingReasons = blocks };
        }

        private static string Recommend(RuntimeEnvironmentInfo environment)
        {
            if (environment.NativeOsArchitecture == CpuArchitecture.X86) return "licensescope-net48-x86";
            if (environment.NativeOsArchitecture == CpuArchitecture.X64) return "licensescope-net48-x64";
            if (environment.NativeOsArchitecture == CpuArchitecture.Arm64 && environment.WindowsVersion.Build < 22000) return "licensescope-net48-x86";
            if (environment.NativeOsArchitecture == CpuArchitecture.Arm64) return "licensescope-net48-x64";
            return string.Empty;
        }
    }

    public static class CurrentPayload
    {
        public static PayloadDescriptor Describe(RuntimeEnvironmentInfo environment) => new PayloadDescriptor { Id = environment.ProcessArchitecture == CpuArchitecture.X86 ? "licensescope-net48-x86" : environment.ProcessArchitecture == CpuArchitecture.X64 ? "licensescope-net48-x64" : "licensescope-net48-anycpu", TargetFramework = "net48", ProcessArchitecture = environment.ProcessArchitecture, RequiredFrameworkVersion = new Version(4, 8), BuildVerified = true, RuntimeVerified = environment.WindowsVersion.Major == 10 && environment.WindowsVersion.Build == 19045 && environment.NativeOsArchitecture == CpuArchitecture.X64 };
    }
}
