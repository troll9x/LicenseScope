# License Scope

License Scope is primarily a read-only desktop license audit for Windows and installed software. It provides a WPF application and the `LicenseScope.Cli.exe` command-line interface, with sanitized JSON, CSV, and standalone HTML reports.

Current scanners cover:

- Windows (`microsoft.windows`)
- Microsoft Office, Project, and Visio (`microsoft.office`)
- Autodesk desktop products (`autodesk.desktop`)
- Adobe desktop products (`adobe.desktop`)
- Trimble SketchUp (`trimble.sketchup`)

Scanner IDs are stable data-contract identifiers and are intentionally independent of product branding.

## Product identity

| Field | Value |
|---|---|
| Display name | License Scope |
| Technical prefix | `LicenseScope` |
| Publisher | Nguyễn Hồng Sơn |
| Repository metadata | `troll9x/LicenseScope` |
| GUI executable | `LicenseScope.App.exe` |
| CLI executable | `LicenseScope.Cli.exe` |
| Installer | `LicenseScope-Setup.exe` |
| Install directory | `C:\Program Files\LicenseScope` |
| Settings directory | `%LOCALAPPDATA%\LicenseScope` |

## Build and test

Requirements: Windows, .NET SDK capable of building .NET Framework 4.8 projects, and the .NET Framework 4.8 targeting pack.

```powershell
dotnet restore LicenseScope.sln
dotnet build LicenseScope.sln -c Debug --no-restore
dotnet test LicenseScope.sln -c Debug --no-build
```

Run the complete local matrix and policy gates with:

```powershell
.\build\Invoke-CI.ps1 -Configuration All -Platforms AnyCPU,x86,x64
```

The installer build additionally requires Inno Setup 7 and the independently verified offline .NET Framework prerequisite described in `installer/prerequisites/README.md`.

## CLI

```powershell
LicenseScope.Cli.exe compatibility
LicenseScope.Cli.exe audit --all
LicenseScope.Cli.exe audit --all --format json,csv,html --output .\reports
```

The audit pipeline is offline and read-only. It does not install or remove keys, activate products, rearm Windows, upload reports, or send telemetry. The GUI offers two separate, explicit remediation paths: **Xóa cấu hình KMS**, which requires typed confirmation and elevation and invokes only Windows `slmgr.vbs /ckms`; and **Gỡ phần mềm**, which is shown only for installed products not confirmed as licensed, requires typed confirmation, and opens the matching vendor uninstaller registered with Windows. Windows itself is never offered for uninstall. Reports exclude machine name by default and sanitize sensitive evidence.

For interface testing without changing the machine, use **Quét file giả lập** and
open `Samples\license-audit-simulation.json`. The bundled fixture contains Adobe,
Autodesk, and SketchUp examples, is visibly marked as simulated, and cannot invoke
the uninstall action.

The **Phân tích dấu vết crack** action performs a strictly read-only,
seven-group Windows trace analysis and emits the same structured verdict to the
GUI, CLI, JSON, CSV, and HTML reports. See
`docs/user-guide/crack-trace-analysis.md` for evidence rules and limitations.

## Documentation

- User guide: `docs/user-guide/`
- Installer and release process: `docs/installer/` and `docs/release/`
- Security and supply chain: `docs/security/`
- Phase audits: `docs/audit/`
- Upstream attribution: `UPSTREAM.md`, `NOTICE.md`, and `PROVENANCE.md`

## Private-development restriction

License Scope continues from the WinLic baseline with user-managed upstream permission for private development. The reachable repository history does not contain the license text referenced by the historical upstream README. This repository therefore does not claim that WinLic is MIT-licensed and does not synthesize a new license for inherited code.

Public source and binary distribution remain outside the scope of this implementation. See `docs/audit/phase-11-upstream-license-audit.md` for the preserved findings.
