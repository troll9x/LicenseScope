# Phase 8 compatibility design

`WinLic.Compatibility` owns OS/framework/native/process/emulation detection, payload description and assessment. It contains no WPF, vendor scanner, installer, network, Registry-write or mutation code. GUI and CLI evaluate compatibility before audit. EOL is a warning; unsupported OS/architecture and insufficient framework block without touching licensing state.

The source manifest describes net48 x86/x64 release candidates, development-only AnyCPU, and blocked net481 ARM64 feasibility. The matrix script builds/tests, PE-inspects and SHA-256 hashes ignored outputs. The tester probe runs compatibility plus a read-only audit on real target machines.

Scanner review: all scanners request explicit Registry views where applicable. Windows/Office system-tool path resolution, Autodesk/Adobe helper architecture and every Arm-emulated/native vendor-tool scenario remain `NEEDS VERIFICATION`. No scanner classification was weakened. New CLI codes are 7 unsupported OS/architecture, 8 insufficient framework and 9 undetermined compatibility; audit meanings remain unchanged.

## Verification evidence

On Windows 10 Home Single Language x64 build 19045 with .NET Framework 4.8.1, standard Debug/Release builds passed and 296 tests passed in each configuration (262 pre-Phase-8 plus 34 new). Release x86 PE `0x014C` and x64 PE `0x8664` both started, returned compatibility exit 0, completed unified audit with identical scanner counts, and exported sanitized JSON/CSV/HTML. GUI x86/x64 displayed the EOL warning, stayed responsive and completed scan. Existing close-to-tray behavior required forced cleanup of only the known smoke processes after graceful close requests; no user process was terminated. Architecture-specific MSTest execution is blocked by test-host bitness, so it is not claimed as passed; full AnyCPU tests plus production payload smoke are the recorded evidence. ARM64 runtime is `BLOCKED_NO_ENVIRONMENT`.
