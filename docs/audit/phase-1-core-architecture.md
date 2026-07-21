# Phase 1 — Core architecture

## 1. Objective

Phase 1 introduces a shared, UI-independent foundation for future read-only license scanners while leaving the existing WPF and PowerShell administration features unchanged. It does not implement a Windows, Office, Autodesk, Adobe, or SketchUp scanner.

## 2. Files created

- `WinLic.sln`
- `src/WinLic.Core/WinLic.Core.csproj`
- contracts under `src/WinLic.Core/Contracts/`
- result/context/progress models under `src/WinLic.Core/Models/`
- orchestration, validation, status mapping, and clock under `src/WinLic.Core/Services/`
- process and system-context runtime infrastructure under `src/WinLic.Core/Runtime/`
- sensitive-data masking under `src/WinLic.Core/Security/`
- MSTest project and unit tests under `tests/WinLic.Core.Tests/`
- this document

## 3. Files changed

- `WinLicApp/WinLicApp.csproj`: references `WinLic.Core`; no WPF source or behavior changed.
- `.gitignore`: ignores MSTest result artifacts.
- `docs/audit/baseline-results.md`: records the already-observed baseline exit codes explicitly without changing the Phase 0 conclusion.

## 4. Project dependencies

```text
WinLicApp (WPF net4.8-windows) ---> WinLic.Core (net48) <--- WinLic.Core.Tests (net48)

WinLicPS (unchanged and independent)
```

`WinLic.Core` has no WPF, PowerShell, scanner implementation, vendor, or test-framework reference. There is no circular dependency.

## 5. Core contracts

- `ILicenseScanner`: stable ID/vendor, applicability, cancellable asynchronous scan.
- `IAuditOrchestrator`: all-scanner execution with cancellation and framework-neutral progress.
- `ISystemContextProvider`: current environment facts.
- `ISystemClock`: deterministic timestamps in tests.
- `IProcessRunner`: direct executable invocation with timeout/cancellation and captured output.
- `IRegistryReader` and `IWmiQueryService`: deliberately read-only seams for Phase 2; no live implementation is introduced in Phase 1.

Scanner registration is explicit constructor composition. Reflection and a dependency-injection framework are unnecessary at this stage.

## 6. Result model

`LicenseStatus` preserves Licensed, Unlicensed, Trial, GracePeriod, Expired, NeedsSignIn, NeedsOnlineVerification, NotInstalled, Unsupported, Unknown, and Error as distinct states. `LicenseStatusMapper` is the sole core mapping to nullable `IsLicensed`: only Licensed is true; Unlicensed and Expired are false; every other state is null.

`LicenseResult` uses typed confidence and expiration values and initializes all collections/strings safely. `AuditResult` retains products and a `ScannerExecutionResult` for every attempted/applicability-checked scanner. Architecture enums model x86, x64, Arm32, Arm64, and Unknown; their existence is not a support claim.

## 7. Orchestrator behavior

`AuditOrchestrator` takes an ordered scanner collection and clock. It checks applicability and runs applicable scanners sequentially. Sequential execution is intentional for this first foundation: it avoids uncontrolled contention over WMI/process/vendor tools and makes cancellation/error ordering deterministic. A later phase may introduce bounded concurrency only with evidence that sources are safe to parallelize.

Products from completed scanners are retained. Null scanner collections are treated as scanner errors rather than silently accepted. Progress carries scanner ID/vendor, one-based index, total count, message, and calculated percentage.

## 8. Exception isolation

Non-cancellation exceptions from applicability or scanning are captured into `ScannerExecutionResult.ErrorType` and `ErrorMessage`. The execution is unsuccessful and later scanners continue. No empty catch is used. Tests cover a successful scanner, empty result, non-applicable scanner, throwing scanner, and a later scanner completing after an earlier failure.

## 9. Cancellation behavior

The orchestrator checks the token before each scanner. An `OperationCanceledException` associated with the requested token produces a cancelled execution, sets `AuditResult.WasCancelled`, retains earlier results, and prevents later scanners from starting. The process runner distinguishes cancellation and timeout and attempts to terminate the child process. Tests use only fake scanners; no licensing command is executed.

## 10. Sensitive-data handling

`SensitiveDataMasker` provides:

- last-five-only product-key display;
- local-part email masking;
- deterministic SHA-256-based machine-name pseudonym;
- replacement of usernames in `C:\Users\...` paths;
- generic redaction for fields containing token, password, secret, credential, cookie, session, or authorization.

Invalid/null inputs do not throw. The helper does not decode, persist, upload, or log secrets. Scanner/report code must call it before evidence leaves its trusted acquisition boundary; automatic report integration is outside Phase 1.

## 11. Test strategy

MSTest was selected because it supports `net48`, integrates with the standard .NET test SDK, and requires only the test SDK, adapter, and framework packages. Tests are deterministic and use fake scanners/clock. They do not call `slmgr`, WMI, Registry, PowerShell, network services, or real vendor software.

Coverage includes every status mapping, masker edge cases, result validation/normalization, architecture label mapping, orchestrator aggregation/isolation/cancellation/progress/time, and stable process-runner request/start-failure behavior.

At implementation time the machine still has no modern `dotnet` SDK or full SDK-aware MSBuild. Consequently package restore, compilation, and test execution are environmental blockers and cannot be represented as passing. Exact command outcomes are recorded in the Phase 1 result.

### Validation command results

| Command | Exit code | Duration | Result |
|---|---:|---:|---|
| `dotnet restore WinLic.sln` | 9009 | 95 ms | `dotnet` not found |
| `dotnet build WinLic.sln -c Debug` | 9009 | 38 ms | `dotnet` not found |
| `dotnet build WinLic.sln -c Release` | 9009 | 37 ms | `dotnet` not found |
| `dotnet test tests\WinLic.Core.Tests\WinLic.Core.Tests.csproj -c Debug` | 9009 | 35 ms | `dotnet` not found; 0 tests executed |
| `dotnet test tests\WinLic.Core.Tests\WinLic.Core.Tests.csproj -c Release` | 9009 | 36 ms | `dotnet` not found; 0 tests executed |
| legacy MSBuild `WinLic.sln /t:Restore` | 1 | 600 ms | `MSB4057`; Restore target unavailable |
| legacy MSBuild solution Debug | 1 | 627 ms | three `MSB4041` errors; SDK-style projects unsupported |
| legacy MSBuild solution Release | 1 | 679 ms | three `MSB4041` errors; SDK-style projects unsupported |
| legacy MSBuild `WinLicApp` Debug | 1 | 327 ms | one `MSB4041` error |
| legacy MSBuild `WinLicApp` Release | 1 | 322 ms | one `MSB4041` error |
| PowerShell parser | 0 | below timer resolution | 0 parser errors |

No compiler warnings or test outcomes are available because compilation/test discovery never started. Static test inventory contains 16 plain test methods and 36 data rows across 8 data-test methods (52 expected cases after discovery), but this is not reported as executed test count.

## 12. Compatibility impact

The core targets `net48`, matching the existing WPF application's .NET Framework generation without adding a WPF dependency or newer BCL requirement. The existing app remains `net4.8-windows`. No OS or architecture support claim changes. `DefaultSystemContextProvider` uses .NET Framework environment APIs and conservative environment-variable mapping; ARM labels still require real-platform verification.

## 13. Not migrated

- Existing WMI, registry, `slmgr`, product-key, KMS, and piracy-audit code remains in `MainWindow.xaml.cs` and `WinLicManager.ps1`.
- Existing mutable `AppSettings` and localization remain unchanged.
- XAML still creates `MainWindow` through `StartupUri`; composition of actual scanners will be introduced only when a scanner exists.
- No administration command is routed through the new process runner.

## 14. Limitations

- No production scanner is registered, so the core is not yet exposed in the UI.
- Process arguments remain a string because .NET Framework 4.8 lacks the newer argument-list API; callers must validate/quote values and must never pass uncontrolled secrets.
- The process runner does not log, but callers remain responsible for preventing secret-bearing arguments.
- Registry and WMI are interfaces only until Phase 2 defines tested Windows implementations.
- Build and tests require a modern SDK-aware toolchain not present on the baseline machine.
- No Windows 7/8/ARM64 compatibility has been verified.

## 15. Phase 2 plan

Phase 2 should implement a read-only Windows scanner behind the new interfaces. It should first add fixture-driven parsers and fake registry/WMI/process providers, then migrate only inspection paths from `MainWindow`. All `/ipk`, `/upk`, `/cpky`, `/rearm`, `/ato`, `/skms`, and `/ckms` operations must remain outside the scanner and unified audit flow.

## Verification gate

Date: 2026-07-21
Status: **BLOCKED — BUILD TOOLCHAIN MISSING**

### Toolchain discovery

- `dotnet.exe`: not found in `PATH`, `C:\Program Files\dotnet`, `C:\Program Files (x86)\dotnet`, or the Visual Studio Build Tools tree.
- `vswhere.exe`: found at `C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe`.
- Visual Studio installation: Visual Studio Build Tools 2026 18.6.0 at `C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools`.
- MSBuild: `C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\MSBuild.exe`.
- MSBuild version: 18.6.3.22110 for .NET Framework.
- `Microsoft.NET.Sdk`: missing. The expected MSBuild SDK directory does not exist.
- .NET Framework reference assemblies/4.8 targeting pack: not found at the standard `Reference Assemblies\Microsoft\Framework\.NETFramework` location.

The Build Tools installation is real and launchable, but it contains only a minimal MSBuild workload. It cannot resolve SDK-style projects.

### Target frameworks

- Before gate: `WinLicApp` used `net4.8-windows`; Core and tests used `net48`.
- After gate: all three projects use `net48`.

`net4.8-windows` was corrected because the project is a .NET Framework 4.8 SDK-style WPF project; WPF remains enabled through `<UseWPF>true</UseWPF>`. This change still requires compiler verification after the missing SDK and targeting pack are installed.

### Restore and build

| Operation | Exit code | Duration | Result |
|---|---:|---:|---|
| MSBuild 18.6 solution restore | 0 | 603 ms | **Not a successful restore**: `NU1503` for all three projects and “Unable to find a project to restore”; all projects were skipped |
| MSBuild 18.6 solution Debug | 1 | 218 ms | `MSB4236`: `Microsoft.NET.Sdk` could not be found |
| MSBuild 18.6 solution Release | 1 | 208 ms | `MSB4236`: `Microsoft.NET.Sdk` could not be found |

The restore exit code must not be interpreted as PASS because zero projects were restored. Compiler and test discovery never started. No per-project builds or tests were attempted after the mandatory missing-toolchain stop condition was established.

### Tests and WPF smoke test

- Debug tests: 0 executed; test runner unavailable.
- Release tests: 0 executed; test runner unavailable.
- WPF smoke test: not run because no executable was produced.
- PowerShell parser: PASS, 0 errors; the interactive script was not executed.

### Static verification

- Core contains no WPF reference, `MessageBox`, `async void`, `Task.Run`, `Thread.Abort`, or empty catch.
- The only new `ProcessStartInfo` is in `ProcessRunner`; it uses `UseShellExecute=false` and does not log arguments.
- License-changing commands remain in the pre-existing GUI/PowerShell administration code and documentation. No such command exists in Core, tests, orchestrator, process-runner tests, or startup changes.
- No `bin`, `obj`, `.vs`, `TestResults`, package cache, or tracked binary artifact was found.

### Required environment components

Before this gate can be resumed, install one supported SDK-aware toolchain configuration:

1. a modern .NET SDK that supplies `Microsoft.NET.Sdk`, plus the .NET Framework 4.8 Developer Pack/Targeting Pack; or
2. modify the existing Visual Studio Build Tools installation to include .NET desktop build tools, the SDK-style .NET build components, and the .NET Framework 4.8 targeting/developer pack.

No installer, SDK download, system PATH change, or registry change was performed by this verification gate. Phase 2 remains blocked.

### Verification rerun after toolchain installation

Date: 2026-07-21
Status: **PASS WITH WARNINGS**

The user installed the missing toolchain manually. The earlier BLOCKED record above is retained as history.

#### Verified toolchain

- `dotnet`: `C:\Program Files\dotnet\dotnet.exe`
- SDK: 10.0.302; SDK MSBuild 18.6.11
- Full MSBuild: `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`, version 18.8.2.30814
- .NET Framework 4.8 references: present at `C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8`
- `Microsoft.NET.Sdk`: present under `C:\Program Files\dotnet\sdk\10.0.302`
- Product targets remain .NET Framework 4.8 (`net48`); .NET 10 is build tooling only.

#### Restore, build, and tests

- Restore: exit 0 in 9.19 seconds; all three projects restored; no `NU1503`, skipped project, warning, or error.
- Initial clean Debug solution build: exit 0, 134 warnings, 0 errors. Initial clean Release had the same warnings.
- Compile fixes: corrected four nullable-flow warnings in `SensitiveDataMasker`; removed unnecessary XML documentation-file generation that caused `CS1591` for every public member while retaining existing type-level XML documentation.
- Final no-incremental Debug solution build: exit 0 in 1.51 seconds; 8 warnings, 0 errors.
- Final no-incremental Release solution build: exit 0 in 1.59 seconds; 8 warnings, 0 errors.
- Individual Debug and Release builds passed for WinLicApp, Core, and the test project.
- Debug tests: 60 discovered, 60 passed, 0 failed, 0 skipped; runner duration 466 ms.
- Release tests: 60 discovered, 60 passed, 0 failed, 0 skipped; runner duration 456 ms.

Additional gate tests cover an empty scanner set, null scanner output, applicability exceptions, preservation of earlier results across cancellation, lowercase/whitespace product-key cases, one-character email local parts, mixed-case user paths on other drives, and distinct machine-name pseudonyms.

#### WPF and compatibility checks

- WPF Debug executable: `WinLicApp\bin\Debug\net48\WinLicApp.exe`.
- Startup smoke test: PASS; process remained alive after 5 seconds and was then stopped without UI interaction.
- `App.xaml`, BAML compilation, icon resources, manifest, localization, and settings code compiled as part of the WPF build.
- PowerShell parser: PASS, 0 errors; script not executed.
- Read-only boundary: 0 license-changing command matches in Core, tests, App startup, or the project file.
- Static quality gate: no empty catch, `async void`, `Thread.Abort`, `Task.Run`, or WPF dependency in Core. The reviewed process runner uses `UseShellExecute=false`.
- Architecture: CorFlags reports PE32, IL-only, `32BITREQ=0`, `32BITPREF=0` for app, Core, and tests. This is AnyCPU/default and is not an ARM64 support claim.

#### Remaining warnings and limitations

Eight nullable warnings remain in the pre-existing `MainWindow.xaml.cs` at lines 532, 633, 1207, 1468, 1522, 1525, 2378, and 2379. They do not prevent compilation or test execution and were not changed because this gate forbids unrelated cleanup of legacy administration code. No Core or test-project warning remains in the final clean builds.

Production scanners, CLI audit, reporting, installer work, explicit x86/x64 payloads, and Windows 7/8/ARM64 verification remain outside Phase 1.
