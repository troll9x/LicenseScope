# Installation

Current 1.0.0 RC artifacts are unsigned; verify the included SHA-256 manifest and SPDX SBOM before installation.

Run `LicenseScope-Setup.exe`, approve UAC, choose English or Vietnamese, and complete the wizard. Setup chooses x86/x64 automatically. The Desktop shortcut is optional.

Windows 8.0 is blocked. Windows 7 requires SP1. Windows 7/8.1 are legacy end-of-support platforms. ARM64 uses x86 emulation and must already have a compatible .NET Framework 4.8-or-later runtime.

Managed silent installation:

```text
LicenseScope-Setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG="C:\private\LicenseScope-install.log"
```

Setup never scans automatically. Settings in `%LOCALAPPDATA%\LicenseScope` and user-selected reports survive upgrade and uninstall. Uninstall does not remove .NET Framework or audited software.

The current artifact is unsigned because no code-signing certificate was provided. Verify SHA-256 against `build-manifest.json` before running it.
