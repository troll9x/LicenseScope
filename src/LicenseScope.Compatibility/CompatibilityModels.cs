using System;
using System.Collections.Generic;

namespace LicenseScope.Compatibility
{
    public enum CpuArchitecture { Unknown, X86, X64, Arm32, Arm64 }
    public enum ProcessExecutionMode { Unknown, NativeX86, NativeX64, NativeArm64, X86OnX64, X86OnArm64, X64OnArm64 }
    public enum CompatibilitySupportLevel { Supported, TechnicallyCompatible, Experimental, Blocked, Unsupported, EndOfLife, Unknown }

    public sealed class RuntimeEnvironmentInfo
    {
        public string WindowsProductName { get; set; } = string.Empty;
        public Version WindowsVersion { get; set; } = new Version(0, 0);
        public string WindowsBuild { get; set; } = string.Empty;
        public CpuArchitecture NativeOsArchitecture { get; set; }
        public CpuArchitecture ProcessArchitecture { get; set; }
        public ProcessExecutionMode ExecutionMode { get; set; }
        public bool Is64BitOperatingSystem { get; set; }
        public bool Is64BitProcess { get; set; }
        public bool IsEmulated { get; set; }
        public Version InstalledNetFrameworkVersion { get; set; } = new Version(0, 0);
        public int? NetFrameworkReleaseKey { get; set; }
        public string FrameworkDetectionSource { get; set; } = string.Empty;
        public bool IsWindowsServer { get; set; }
    }

    public sealed class ArchitectureProbeResult
    {
        public CpuArchitecture NativeArchitecture { get; set; }
        public CpuArchitecture ProcessArchitecture { get; set; }
        public ProcessExecutionMode ExecutionMode { get; set; }
        public bool IsEmulated { get; set; }
        public string Warning { get; set; } = string.Empty;
    }

    public interface IArchitectureProbe { ArchitectureProbeResult ProbeCurrentProcess(); }

    public sealed class PayloadDescriptor
    {
        public string Id { get; set; } = string.Empty;
        public string TargetFramework { get; set; } = string.Empty;
        public CpuArchitecture ProcessArchitecture { get; set; }
        public Version RequiredFrameworkVersion { get; set; } = new Version(4, 8);
        public bool BuildVerified { get; set; }
        public bool RuntimeVerified { get; set; }
    }

    public sealed class CompatibilityAssessment
    {
        public bool CanStart { get; set; }
        public bool CanRunAudit { get; set; }
        public CompatibilitySupportLevel SupportLevel { get; set; }
        public bool IsEndOfLife { get; set; }
        public bool RuntimeVerified { get; set; }
        public string RecommendedPayloadId { get; set; } = string.Empty;
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> BlockingReasons { get; set; } = Array.Empty<string>();
    }

    public interface ICompatibilityEvaluator
    {
        CompatibilityAssessment Evaluate(RuntimeEnvironmentInfo environment, PayloadDescriptor payload);
    }
}
