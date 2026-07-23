# Phase 11 License Scope rebranding inventory

Status: implementation authorized for private development under user-managed upstream permission. Public distribution remains deferred.

## Approved target identity

| Field | Target |
|---|---|
| Display name | License Scope |
| Technical name / namespace prefix | LicenseScope |
| Repository | `troll9x/LicenseScope` (Private) |
| Publisher display name | Nguyễn Hồng Sơn |
| CLI | `LicenseScope.Cli.exe` |
| Installer | `LicenseScope-Setup.exe` |
| Install/settings directory | `LicenseScope` |
| Description | Read-only desktop license audit for Windows and installed software |

Installer AppId: `{0C329754-97C0-45BE-8664-347B26EDA0E0}`. Test-install AppId: `{98FB3801-EC81-49A7-987A-7742D6D01687}`. Neither reuses a WinLic identity.

## Inventory summary

The scoped search found `WinLic` in 205 files, `WinLicApp` in 28, `WinLicAudit` in 14, `WinLicPS` in 10, and `ardennguyen` in 8. Counts include historical audit documentation and tests; therefore they are not a blind replacement list.

| Category | Examples | Required treatment |
|---|---|---|
| MUST_REBRAND | WPF title/About text, report name/title/generator, CLI banner/output filename, settings/report paths, installer AppName/path/group/filename/AppId, release filenames and SBOM identity | Change to the approved License Scope identity after the license gate passes |
| INTERNAL_TECHNICAL_RENAME | Solution, project directories and files, namespaces, assemblies, project references, XAML classes, tests and build paths | Migrate with Git-aware moves and compile after each bounded group |
| MUST_KEEP_FOR_ATTRIBUTION | Upstream name/URL, baseline commits and provenance evidence | Keep only in future audited attribution and historical evidence |
| STABLE_SCANNER_ID | `microsoft.windows`, `microsoft.office`, `autodesk.desktop`, `adobe.desktop`, `trimble.sketchup` | Do not rename for branding |
| HISTORICAL_DOCUMENTATION | Phase 0–10 evidence describing WinLic paths and behavior at that time | Preserve historical truth; label as historical rather than rewriting evidence |
| TEST_FIXTURE | Inputs that intentionally verify old-name isolation or attribution allowlists | Keep only when explicit and documented |
| GENERATED_ARTIFACT | Setup, bundle, SBOM, checksums, manifests and reports under ignored artifact paths | Regenerate; never commit |

## Planned migration map

- `WinLic.sln` → `LicenseScope.sln`.
- `WinLic.Core/Application/Reporting/Compatibility` → corresponding `LicenseScope.*` projects.
- Vendor scanner projects → `LicenseScope.Windows`, `.Office`, `.Autodesk`, `.Adobe`, `.SketchUp` while retaining scanner IDs.
- `WinLic.Cli` / `WinLicAudit.Cli.exe` → `LicenseScope.Cli` / `LicenseScope.Cli.exe`.
- `WinLicApp` / `WinLicApp.exe` → `LicenseScope.App` / a LicenseScope-branded GUI executable.
- `%LOCALAPPDATA%\WinLic` → `%LOCALAPPDATA%\LicenseScope`; no automatic import.
- `Program Files\WinLic` → `Program Files\LicenseScope`.
- WinLic installer AppId/uninstall identity → a newly generated independent AppId/uninstall identity.
- WinLic release/SBOM/checksum/manifest names → the approved LicenseScope names.
- `ardennguyen/WinLic` production links → `troll9x/LicenseScope`; upstream URL remains only in audited attribution/evidence.

## Mandatory coexistence controls

Static tests must prove separate AppId, install directory, settings/reports, uninstall key, Start Menu group, executable/process names and cleanup allowlists. Runtime smoke must place protected WinLic canaries outside License Scope paths and prove install/uninstall leaves them untouched. A real side-by-side WinLic runtime test remains `BLOCKED_TEST_FIXTURE_UNAVAILABLE` unless an authorized fixture is supplied; no binary will be downloaded for it.

## Security contracts that must not change

The unified product remains read-only. Rebranding must not expose or call activation, key installation/removal, rearm, KMS mutation, vendor-state mutation, raw-key export, telemetry, report upload, credential/token collection, or scanner-wide administrator execution. Legacy administration code is not a License Scope production feature.

## Remote state

The read-only probe of `https://github.com/troll9x/LicenseScope.git` returned `Repository not found` for the current credentials. No `origin` is configured, no repository was created, and no push was attempted. `winlic-upstream` remains the sole reference remote.
