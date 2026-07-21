# Compatibility risks and provisional matrix

## What the repository proves

The project declares only `net4.8-windows`; it does not declare CPU targets or produce per-architecture payloads in this baseline environment. README claims Windows 10 (1903+) and Windows 11 x64 for the GUI, but there is no automated compatibility test or release workflow in the repository. Source and README are therefore insufficient to claim the full requested matrix.

`app.manifest` declares compatibility GUIDs for Windows 10 and Windows 8.1. Absence/presence of a GUID is not by itself proof of runtime compatibility, but it is a concrete gap for the requested legacy matrix.

## Provisional compatibility matrix

Every row beyond the documented Windows 10/11 x64 intent is `NEEDS VERIFICATION`; even the documented rows were not build/run verified during this baseline because the SDK is unavailable.

| OS | Minimum | OS arch | Current app payload | Framework | Native/emulation | Test status | Known limitation |
|---|---|---:|---|---|---|---|---|
| Windows 7 | SP1 | x86 | unspecified/AnyCPU intent | .NET Framework 4.8 | expected x86 | NEEDS VERIFICATION | no manifest declaration; prerequisite servicing/TLS/API behavior untested |
| Windows 7 | SP1 | x64 | unspecified/AnyCPU intent | .NET Framework 4.8 | expected x64 | NEEDS VERIFICATION | same; no x64 artifact proof |
| Windows 8 | base | x86 | unspecified/AnyCPU intent | .NET Framework 4.8 | expected x86 | NEEDS VERIFICATION | no manifest declaration or test |
| Windows 8 | base | x64 | unspecified/AnyCPU intent | .NET Framework 4.8 | expected x64 | NEEDS VERIFICATION | no manifest declaration or test |
| Windows 8.1 | base | x86 | unspecified/AnyCPU intent | .NET Framework 4.8 | expected x86 | NEEDS VERIFICATION | GUID exists; build/runtime still untested |
| Windows 8.1 | base | x64 | unspecified/AnyCPU intent | .NET Framework 4.8 | expected x64 | NEEDS VERIFICATION | GUID exists; build/runtime still untested |
| Windows 10 | README says 1903+ | x86 | unspecified/AnyCPU intent | .NET Framework 4.8 | expected x86 | NEEDS VERIFICATION | README GUI table says x64; no x86 build/test |
| Windows 10 | README says 1903+ | x64 | unspecified/AnyCPU intent | .NET Framework 4.8 | expected x64 | NOT RUN | baseline host is Win10 x64, but app could not be built |
| Windows 10 | requested | ARM64 | no ARM64 payload | undetermined | possible x86 fallback | NEEDS VERIFICATION | .NET Framework/WPF native ARM64 feasibility and x86 emulation must be verified officially and on hardware |
| Windows 11 | base | x64 | unspecified/AnyCPU intent | .NET Framework 4.8 | expected x64 | NEEDS VERIFICATION | documented intent only; no current build artifact/test |
| Windows 11 | base | ARM64 | no ARM64 payload | undetermined | possible x86/x64 fallback | NEEDS VERIFICATION | native WPF target and emulation behavior unverified |

“Expected” describes CLR/AnyCPU intent, not a support claim.

## Major compatibility risks

1. **Target framework versus legacy OS:** .NET Framework 4.8 deployment prerequisites and supported servicing state for Windows 7 SP1/8/8.1 must be checked against current official Microsoft redistribution/support documentation. `NEEDS VERIFICATION`.
2. **ARM64:** the current .NET Framework 4.8 WPF project has no ARM64 target. Native ARM64 feasibility, .NET Framework 4.8 versus 4.8.1 requirements, and Windows 10/11 ARM emulation must be proven with official documentation and real devices/VMs. `NEEDS VERIFICATION`.
3. **AnyCPU ambiguity:** without explicit `PlatformTarget` and release builds, native bitness and registry/WMI view behavior are not controlled. This matters for Office/vendor detection and 32/64-bit registry views.
4. **SDK-style build dependency:** the installed legacy MSBuild cannot build the project; release builds require a modern .NET SDK or Visual Studio Build Tools capable of SDK-style .NET Framework projects.
5. **C# language/runtime gap:** `LangVersion=latest` permits compiler features beyond the age of legacy OSes. Compatibility must be validated at build and runtime, not inferred from target framework alone.
6. **DPI:** WPF is used, but the manifest has no explicit DPI-awareness declaration and there is no DPI test.
7. **Localized Windows:** command-output parsing contains English-label assumptions.
8. **Privileges and filesystem:** settings beside an installed executable conflict with standard-user writes under `Program Files`.
9. **Framework bootstrap:** checking .NET from WPF startup cannot handle a truly missing framework. Installer/bootstrapper prerequisite detection is required.
10. **Architecture-sensitive evidence:** registry redirection, `System32` behavior, Office bitness, and external tool architecture require explicit tests for every payload.

## Recommended Phase 8 validation strategy

- First establish explicit x86 and x64 builds and tests on Windows 10/11.
- Create separate legacy hosts only after confirming a supported target/toolchain for Windows 7 SP1/8/8.1.
- Treat ARM64 native as unavailable until a minimal WPF probe plus full application build/run is verified on both Windows 10 ARM64 and Windows 11 ARM64.
- If native ARM64 is not viable, choose an explicit x86 fallback (especially for Windows 10 ARM64) only after testing WMI, registry views, child tools, and installer detection under emulation.
- Record each tested OS build, framework release value, app payload architecture, and evidence-source result. Do not promote a row from `NEEDS VERIFICATION` based on compilation alone.
