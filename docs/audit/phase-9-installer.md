# Phase 9 — Universal offline installer

The release is one x86-compatible Inno Setup bootstrapper containing explicit x86 and x64 .NET Framework 4.8 payloads and the verified offline runtime. Native x64 selects x64; Windows x86 and Windows 10/11 ARM64 select x86. There is no ARM64-native WPF claim.

Windows 8.0 is blocked. Windows 7 requires SP1. Windows 7 SP1 and Windows 8.1 show end-of-support warnings. On ARM64, Setup requires an already-present compatible .NET Framework runtime and never executes the x86/x64 prerequisite.

Production Setup is per-machine, requests elevation, installs to Program Files, creates Start Menu links and offers an unchecked Desktop link. It never starts an audit automatically. Its optional post-install GUI launch is de-elevated and skipped in silent mode. A stable AppId supports upgrades and blocks downgrades. Uninstall does not remove .NET, vendor software, reports, or `%LOCALAPPDATA%\WinLic` settings.

## Evidence and limitations

- Inno Setup 7.0.2 compiled the production source.
- The supplied .NET 4.8 package was verified by size, SHA-256, metadata, Valid Authenticode status, and exact Microsoft signer before every build.
- Windows 10 x64 smoke variants exercised x64 and forced-x86 routing, GUI startup, CLI audit, JSON/CSV/HTML reports, settings preservation, and uninstall.
- The final production Setup was installed per-machine through UAC to `C:\Program Files\WinLic`, selected the native x64 payload, ran GUI and CLI under a standard-user medium-integrity token, completed unified GUI/CLI scans, and was uninstalled through UAC with exit code 0.
- Upgrade 0.9.0.0 to 1.0.0.0 and downgrade rejection were exercised.
- Production uninstall removed application files, Start Menu shortcuts, and the Registry32 uninstall entry while retaining synthetic LocalAppData settings and user report markers. The .NET release key remained 533325 and no framework installer process ran.
- Framework-missing and reboot paths are `BLOCKED_NO_ENVIRONMENT`; the host release key is 533325.
- ARM64, Windows 7 SP1, and Windows 8.1 runtime tests remain `NEEDS VERIFICATION` on representative machines.
- Final installer status: `UNSIGNED — CODE SIGNING CERTIFICATE NOT PROVIDED`.

Static scans reject activation commands, online download logic, source, tests and PDBs. Installer execution never launches an audit or changes licensing state.
