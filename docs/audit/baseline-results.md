# Baseline results

## Host and toolchain

- Host reported by the process: Windows `10.0.19045`, AMD64, 64-bit OS and process.
- Installed .NET Framework registry values: Release `533325`, version `4.8.09037`.
- Windows PowerShell: 5.1-compatible system executable (`10.0.19041.3996`).
- `pwsh`: not found.
- `dotnet`: not found.
- Visual Studio 2022 Build Tools MSBuild: not found at the standard checked path.
- Legacy .NET Framework MSBuild 4.8.9037 exists in Framework/Framework64.

## Build results

| Configuration | Command | Result |
|---|---|---|
| Debug | `dotnet build WinLicApp\WinLicApp.csproj -c Debug --no-restore` | Exit code `1`; BLOCKED: `dotnet` command not installed |
| Debug | Framework64 MSBuild `/t:Build /p:Configuration=Debug` | Exit code `1`; FAIL: `MSB4041`; legacy MSBuild cannot load this SDK-style project |
| Release | Framework64 MSBuild `/t:Build /p:Configuration=Release` | Exit code `1`; FAIL: `MSB4041`; same toolchain incompatibility |

The `MSB4041` result occurs while loading the project, before compilation. It is not evidence of a C# source defect. No build outputs were created.

## Test results

- Automated test projects discovered: 0.
- Automated tests executed: 0.
- Passed: 0.
- Failed: 0.
- Skipped: 0.
- PowerShell syntax baseline: `WinLicManager.ps1` parsed using `System.Management.Automation.Language.Parser`; 0 parser errors.
- GUI startup, CLI interactive execution, Windows license queries, architecture variants, and report export were not run. Running the interactive licensing manager could expose or mutate real machine licensing state and is not an appropriate automated baseline without isolation.

## Static baseline findings

- One WPF production project; no solution and no tests.
- One independent interactive PowerShell script.
- No CI/build/release workflow and no installer/release script in the checkout.
- No JSON/CSV/HTML reporting implementation.
- No scanner architecture or shared GUI/CLI engine.
- Office UI is a placeholder; no other vendor scanner exists.
- License-changing commands are confirmed in both application paths.
- Default audit behavior cannot yet be guaranteed read-only because read-only and administrative features share the same monolithic hosts, although individual actions require user selection/confirmation.

## Phase 0 acceptance

Discovery documentation is complete. Build verification is blocked by the missing modern SDK and there are no tests to run. Phase 1 should not begin until this baseline is reviewed; before any Phase 1 commit, install/use an approved modern build toolchain and obtain a successful untouched baseline build, or explicitly accept the documented toolchain blocker.
