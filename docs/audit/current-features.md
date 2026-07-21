# Current features and behavior

## Implemented in the WPF application

- Bilingual English/Vietnamese UI and settings/about dialogs.
- Windows system and activation information from WMI, registry, and `slmgr`.
- OEM BIOS key, registry backup key, installed-key decoding, partial key, activation channel/status, rearm count, KMS host/settings, and selected grace/expiration evidence.
- Configurable heuristic scan for third-party activation traces across services, scheduled tasks, processes, files, ports, registry, licensing state, timestamps, and event logs.
- Settings update downloaded from the repository and release-update check.
- Deferred elevation via UAC for selected operations.
- Key masking in selected display paths, with a UI option capable of showing a full key.

## Implemented in the PowerShell application

The interactive script broadly mirrors the GUI's eight options and has EN/VI strings, settings parsing, WMI/registry/`slmgr` inspection, third-party activation heuristics, and elevation/relaunch behavior. It requires PowerShell and manual menu interaction; it is not a unified audit CLI.

## License-changing features confirmed in source

The following are present and can change machine licensing state:

- Install a product key with `slmgr /ipk`.
- Trigger online activation with `slmgr /ato`.
- Remove the installed key with `slmgr /upk`.
- Remove the registry key with `slmgr /cpky`.
- Rearm licensing with `slmgr /rearm` and optionally restart Windows.
- Install a GVLK/change activation channel.
- Set a KMS host with `slmgr /skms`.
- Clear KMS settings with `slmgr /ckms`.

These paths are mixed into the same main window/menu as read-only inspection. The future public audit workflow must never invoke them.

## Not implemented

- General `ILicenseScanner` contract and orchestrator.
- Structured `LicenseStatus`, `LicenseResult`, evidence, warning, or confidence models.
- “Scan all software” GUI workflow, result grid, filters, progress, cancellation, or retry.
- Unified CLI command such as `audit --all`.
- JSON, CSV, or HTML reports.
- Default masking for email, SID, Machine GUID, username-bearing paths, or general evidence fields.
- Office scanner. The Office tab explicitly shows “under development.”
- Microsoft 365, Project, Visio, Autodesk, Adobe, SketchUp, or additional vendor scanners.
- Installer/bootstrapper, offline prerequisites, uninstaller, or payload selection.
- Dedicated portable packaging logic or repository release automation.
- Automated unit/integration/UI tests.
- Scanner exception isolation at an orchestrator boundary.
- Verified x86/x64/ARM64/legacy builds.

## Evidence-quality concerns

- Some `slmgr` parsing searches English labels such as `License Status`, `Name`, `Description`, `KMS machine name`, `Error`, or `successfully`; this is not proven robust on non-English Windows.
- Several piracy conclusions rely on heuristic combinations and lists that include references to third-party activation projects. Their accuracy and provenance need formal review; installation or a generic key alone must not become an `Unlicensed` conclusion.
- Full product keys can be decoded/read and shown. Future audit models must retain only a masked/partial representation by default and ensure raw values never enter logs/reports.
- Update/settings code performs network access, while the requested audit must work offline and must distinguish loss of network from unlicensed status.
- No evidence currently establishes vendor-supported licensing semantics for Office, Autodesk, Adobe, or SketchUp. All such support remains `NEEDS VERIFICATION`.
