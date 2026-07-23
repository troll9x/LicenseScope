using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LicenseScope.Compatibility
{
    public sealed class WindowsArchitectureProbe : IArchitectureProbe
    {
        private const ushort Unknown = 0, I386 = 0x014c, Amd64 = 0x8664, Arm = 0x01c4, Arm64 = 0xaa64;

        public ArchitectureProbeResult ProbeCurrentProcess()
        {
            try
            {
                ushort processMachine, nativeMachine;
                try
                {
                    if (IsWow64Process2(Process.GetCurrentProcess().Handle, out processMachine, out nativeMachine))
                    {
                        var native = Map(nativeMachine);
                        var process = processMachine == Unknown ? native : Map(processMachine);
                        return Create(process, native);
                    }
                }
                catch (EntryPointNotFoundException) { }
                catch (DllNotFoundException) { }

                NativeMethods.SYSTEM_INFO info;
                NativeMethods.GetNativeSystemInfo(out info);
                var nativeFallback = MapProcessorArchitecture(info.wProcessorArchitecture);
                bool wow64;
                if (!NativeMethods.IsWow64Process(Process.GetCurrentProcess().Handle, out wow64))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                var processFallback = wow64 ? CpuArchitecture.X86 : (Environment.Is64BitProcess ? nativeFallback : CpuArchitecture.X86);
                return Create(processFallback, nativeFallback);
            }
            catch (Exception ex) when (ex is Win32Exception || ex is EntryPointNotFoundException || ex is DllNotFoundException)
            {
                var process = Environment.Is64BitProcess ? CpuArchitecture.X64 : CpuArchitecture.X86;
                var native = Environment.Is64BitOperatingSystem ? CpuArchitecture.X64 : CpuArchitecture.X86;
                var result = Create(process, native);
                result.Warning = "Native architecture APIs were unavailable; conservative fallback was used.";
                return result;
            }
        }

        public static CpuArchitecture Map(ushort machine) => machine == I386 ? CpuArchitecture.X86 : machine == Amd64 ? CpuArchitecture.X64 : machine == Arm ? CpuArchitecture.Arm32 : machine == Arm64 ? CpuArchitecture.Arm64 : CpuArchitecture.Unknown;

        public static ArchitectureProbeResult Create(CpuArchitecture process, CpuArchitecture native)
        {
            var mode = ProcessExecutionMode.Unknown;
            if (process == CpuArchitecture.X86 && native == CpuArchitecture.X86) mode = ProcessExecutionMode.NativeX86;
            else if (process == CpuArchitecture.X64 && native == CpuArchitecture.X64) mode = ProcessExecutionMode.NativeX64;
            else if (process == CpuArchitecture.Arm64 && native == CpuArchitecture.Arm64) mode = ProcessExecutionMode.NativeArm64;
            else if (process == CpuArchitecture.X86 && native == CpuArchitecture.X64) mode = ProcessExecutionMode.X86OnX64;
            else if (process == CpuArchitecture.X86 && native == CpuArchitecture.Arm64) mode = ProcessExecutionMode.X86OnArm64;
            else if (process == CpuArchitecture.X64 && native == CpuArchitecture.Arm64) mode = ProcessExecutionMode.X64OnArm64;
            return new ArchitectureProbeResult { ProcessArchitecture = process, NativeArchitecture = native, ExecutionMode = mode, IsEmulated = mode == ProcessExecutionMode.X86OnArm64 || mode == ProcessExecutionMode.X64OnArm64 };
        }

        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);

        private static CpuArchitecture MapProcessorArchitecture(ushort value) => value == 0 ? CpuArchitecture.X86 : value == 9 ? CpuArchitecture.X64 : value == 5 ? CpuArchitecture.Arm32 : value == 12 ? CpuArchitecture.Arm64 : CpuArchitecture.Unknown;

        private static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)] internal struct SYSTEM_INFO { internal ushort wProcessorArchitecture, wReserved; internal uint dwPageSize; internal IntPtr lpMinimumApplicationAddress, lpMaximumApplicationAddress; internal IntPtr dwActiveProcessorMask; internal uint dwNumberOfProcessors, dwProcessorType, dwAllocationGranularity; internal ushort wProcessorLevel, wProcessorRevision; }
            [DllImport("kernel32.dll")] internal static extern void GetNativeSystemInfo(out SYSTEM_INFO info);
            [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool IsWow64Process(IntPtr process, out bool wow64);
        }
    }
}
