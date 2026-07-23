# Current License Scope architecture

## Solution

`LicenseScope.sln` contains 20 C# projects: ten production projects, the WPF application, the CLI, and nine test projects. The solution retains its original project GUIDs and Any CPU, x86, and x64 configurations.

```text
LicenseScope.App ─┐
LicenseScope.Cli ─┼─> LicenseScope.Application ─> scanner implementations
                  ├─> LicenseScope.Reporting
                  ├─> LicenseScope.Compatibility
                  └─> LicenseScope.Core
```

The scanner projects are `LicenseScope.Windows`, `LicenseScope.Office`, `LicenseScope.Autodesk`, `LicenseScope.Adobe`, and `LicenseScope.SketchUp`. Their public scanner IDs remain stable and are not product-brand identifiers.

## Production identity

- Display name: License Scope
- GUI: `LicenseScope.App.exe`
- CLI: `LicenseScope.Cli.exe`
- Installer: `LicenseScope-Setup.exe`
- Settings: `%LOCALAPPDATA%\LicenseScope`
- Repository metadata: `troll9x/LicenseScope`

The installer uses an independent AppId and install/uninstall identity. It does not migrate or remove WinLic data.

## Read-only audit pipeline

The unified pipeline registers scanners in stable order, runs them through `AuditOrchestrator`, sanitizes results, and renders JSON, CSV, or HTML. Scanner failures are isolated. Evidence acquisition uses fixed read-only commands and registry/filesystem metadata; reports mask sensitive fields and exclude machine name by default.

Activation, product-key installation/removal, rearm, KMS mutation, telemetry, and report upload are outside the unified production audit contract. The historical upstream PowerShell implementation is retained under `upstream/` for provenance only and is excluded from the solution, installer, CI parser set, and release payload.

## Historical documents

Phase 0–10 audit documents describe the repository under its name and paths at the time. Those references remain unchanged as historical evidence. Phase 11 provenance and rebranding documents record the transition to License Scope.
