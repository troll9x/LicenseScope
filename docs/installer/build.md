# Building the installer

Prerequisites are Windows, a toolchain capable of building `net48`, Inno Setup 7, and the exact offline runtime described by `installer/prerequisites/dotnet48-runtime.json` in the ignored `cache` folder.

```powershell
.\build\Build-Installer.ps1
.\build\Test-Installer.ps1
```

The build verifies the prerequisite, restores, tests, builds explicit x86/x64 PE payloads, stages an allowlist, compiles Setup, scans content, and writes `artifacts/installer/build-manifest.json` with size and SHA-256. Artifacts, staging and framework cache are not committed.

Official references reviewed:

- https://learn.microsoft.com/dotnet/framework/deployment/deployment-guide-for-developers
- https://learn.microsoft.com/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
- https://dotnet.microsoft.com/dotnet_library_license.htm
- https://jrsoftware.org/ishelp/topic_setup_architecturesallowed.htm
- https://jrsoftware.org/ishelp/topic_64bit.htm
- https://jrsoftware.org/ishelp/topic_purchase.htm

Microsoft documents `/q /norestart /ChainingPackage <name>` for chained offline deployment. Exit codes 1641 and 3010 indicate restart. Redistribution remains subject to Microsoft's license terms. Commercial Inno Setup users should follow its publisher's purchase policy.
