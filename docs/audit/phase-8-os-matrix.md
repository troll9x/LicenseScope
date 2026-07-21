# Phase 8 OS and architecture matrix

Evidence date: 2026-07-21. Build evidence never implies runtime verification.

| OS | Edition/build | Native | Payload/TFM | Process/mode | Framework | Build | Runtime | GUI | CLI/audit | Scanners/reports | UAC | Result | Limitations |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Windows 7 SP1 x86 | NOT_TESTED | X86 | x86/net48 | X86/NativeX86 | 4.8 required | PASS | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | BLOCKED_NO_ENVIRONMENT | EOL; VM absent |
| Windows 7 SP1 x64 | NOT_TESTED | X64 | x64/net48 | X64/NativeX64 | 4.8 required | PASS | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | BLOCKED_NO_ENVIRONMENT | EOL; VM absent |
| Windows 8.0 x86 | 6.2/9200 | X86 | none | NOT_APPLICABLE | net48 unsupported | NOT_APPLICABLE | NOT_APPLICABLE | NOT_APPLICABLE | NOT_APPLICABLE | NOT_APPLICABLE | NOT_APPLICABLE | UNSUPPORTED | Not a net48 target |
| Windows 8.0 x64 | 6.2/9200 | X64 | none | NOT_APPLICABLE | net48 unsupported | NOT_APPLICABLE | NOT_APPLICABLE | NOT_APPLICABLE | NOT_APPLICABLE | NOT_APPLICABLE | NOT_APPLICABLE | UNSUPPORTED | Not a net48 target |
| Windows 8.1 x86 | 6.3/9600 | X86 | x86/net48 | X86/NativeX86 | 4.8 required | PASS | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | BLOCKED_NO_ENVIRONMENT | EOL; VM absent |
| Windows 8.1 x64 | 6.3/9600 | X64 | x64/net48 | X64/NativeX64 | 4.8 required | PASS | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | BLOCKED_NO_ENVIRONMENT | EOL; VM absent |
| Windows 10 x86 | NOT_TESTED | X86 | x86/net48 | X86/NativeX86 | 4.8 required | PASS | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | BLOCKED_NO_ENVIRONMENT | EOL; target absent |
| Windows 10 x64 | Pro 22H2/19045 | X64 | x86+x64/net48 | X86OnX64 + NativeX64 | 4.8.1 | PASS | PASS | PASS | PASS | PASS | PASS | PASS_WITH_WARNINGS | Tested machine; OS EOL |
| Windows 10 ARM64 + x86 | NOT_TESTED | ARM64 | x86/net48 | X86/X86OnArm64 | 4.8 required | PASS | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | BLOCKED_NO_ENVIRONMENT | Device absent |
| Windows 11 x64 | NOT_TESTED | X64 | x64/net48 | X64/NativeX64 | 4.8 required | PASS | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | BLOCKED_NO_ENVIRONMENT | Environment absent |
| Windows 11 ARM64 + x86 | NOT_TESTED | ARM64 | x86/net48 | X86/X86OnArm64 | 4.8 required | PASS | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | BLOCKED_NO_ENVIRONMENT | Device absent |
| Windows 11 ARM64 + x64 | NOT_TESTED | ARM64 | x64/net48 | X64/X64OnArm64 | 4.8 required | PASS | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | BLOCKED_NO_ENVIRONMENT | Device absent |
| Windows 11 ARM64 native | NOT_TESTED | ARM64 | ARM64/net481 | ARM64/NativeArm64 | 4.8.1 required | BLOCKED_NO_ENVIRONMENT | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | NOT_TESTED | BLOCKED_NO_ENVIRONMENT | No host/device |
