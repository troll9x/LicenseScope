# Phase 11 License Scope rebranding

Status: implementation complete for private development only.

## Approved identity

| Field | Value |
|---|---|
| Display name | License Scope |
| Technical prefix | `LicenseScope` |
| Publisher | Nguyễn Hồng Sơn |
| Repository metadata | `troll9x/LicenseScope` |
| GUI | `LicenseScope.App.exe` |
| CLI | `LicenseScope.Cli.exe` |
| Installer | `LicenseScope-Setup.exe` |
| Install directory | `C:\Program Files\LicenseScope` |
| Settings directory | `%LOCALAPPDATA%\LicenseScope` |
| Installer AppId | `{0C329754-97C0-45BE-8664-347B26EDA0E0}` |
| Test installer AppId | `{98FB3801-EC81-49A7-987A-7742D6D01687}` |

## Structural result

`WinLic.sln` was moved to `LicenseScope.sln`. All 20 C# projects retain their original project GUIDs and the Any CPU, x86, and x64 solution configurations. Production and test projects now use LicenseScope project names, paths, namespaces, assemblies, XAML class names, and references.

The stable scanner IDs remain `microsoft.windows`, `microsoft.office`, `autodesk.desktop`, `adobe.desktop`, and `trimble.sketchup`.

## Isolation and security

The installer uses independent production and test AppIds, executable names, install directory, settings directory, Start Menu identity, and uninstall registry identity. It contains no migration from WinLic and no cleanup rule for WinLic paths. Static coexistence policy and installer smoke canaries guard this boundary.

Phase 11 separates the legacy administrative WPF surface and historical PowerShell implementation under `upstream/`, where they are excluded from the solution, CI parser set, installer payload, and release automation. The production WPF application now exposes only Scan All, cancellation, filtering, sanitized report export, and attribution. Static source and installer policies reject activation, key installation/removal, rearm, KMS mutation, telemetry, and report upload behavior.

## Distribution restriction

This work is private development under user-managed upstream permission. It does not claim MIT terms. Public source and binary distribution remain deferred as documented in the upstream license audit.

## Validation result

| Check | Result |
|---|---|
| `dotnet restore LicenseScope.sln` | PASS |
| Debug build | PASS, 0 errors |
| Release build | PASS, 0 errors |
| Debug tests | PASS, 296 passed, 0 failed |
| Release tests | PASS, 296 passed, 0 failed |
| Any CPU / x86 / x64 | PASS |
| Installer policy | PASS, 16 checks |
| Prerequisite manifest | PASS, 9 checks |
| Installer coexistence policy | PASS, 8 checks |
| Rebranding/read-only policy | PASS, 13 checks |
| Release policy | PASS, 16 checks |
| PowerShell parser | PASS, 18 files |
| `git diff --check` | PASS; line-ending normalization warnings only |

Full installer compilation and runtime smoke were not run because the verified offline .NET Framework 4.8 redistributable is absent from `installer/prerequisites/cache`. A real side-by-side WinLic runtime fixture was not supplied. Static coexistence checks pass, and the smoke script now protects a WinLic canary when that fixture-independent smoke can run.
