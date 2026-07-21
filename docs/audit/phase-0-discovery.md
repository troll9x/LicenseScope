# Phase 0 — Repository discovery

Date: 2026-07-21
Branch: `feat/unified-license-audit`
Baseline commit: `bf8dd86 docs: update README for v1.5`

## Scope and method

Phase 0 was read-only with respect to application source. Every tracked source/configuration file was inventoried. The project file, application entry point, manifest, WPF views/code-behind, localization/settings implementation, PowerShell implementation, README, ignore rules, and Git history were inspected. Searches were also made for solutions, tests, CI workflows, installer/release scripts, package/project references, architecture targets, and license-changing commands.

The only files added in this phase are the five documents under `docs/audit/` required by the master prompt. No application source, dependency, installer, or generated artifact was changed.

## Repository inventory

```text
WinLic/
├── .gitignore
├── README.md
├── WinLicApp/
│   ├── AboutDialog.xaml[.cs]
│   ├── App.xaml[.cs]
│   ├── AppSettings.cs
│   ├── AssemblyInfo.cs
│   ├── InputDialog.xaml[.cs]
│   ├── Localization.cs
│   ├── MainWindow.xaml[.cs]
│   ├── SettingsDialog.xaml[.cs]
│   ├── app.manifest
│   ├── WinLicApp.csproj
│   ├── winlic.ico
│   └── winlic_256.png
└── WinLicPS/
    ├── WinLicManager.ps1
    ├── settings.default.ini
    └── settings.ini
```

No `.sln`, test project, `.github/workflows`, installer definition, bootstrapper, release script, package lock, `Directory.Build.*`, or `AGENTS.md` is tracked at the baseline commit.

## Exact target and dependencies

- `WinLicApp.csproj` is an SDK-style WPF executable project.
- Target framework: `net4.8-windows` (.NET Framework 4.8).
- Output type: `WinExe`.
- Nullable annotations and `LangVersion=latest` are enabled.
- Direct framework references: `System.Management` and `System.Net.Http`.
- No NuGet `PackageReference` and no `ProjectReference` exist.
- No `PlatformTarget`, `Platforms`, `RuntimeIdentifier`, `RuntimeIdentifiers`, or `Prefer32Bit` is declared. Therefore the repository does not define or prove distinct x86, x64, or ARM64 payloads.

## Files inspected

- `README.md`, `.gitignore`
- all files in `WinLicApp/`, including complete WPF views and code-behind
- all files in `WinLicPS/`, including both INI data sets and the complete script
- Git branch/status/log and tracked/untracked inventory

Binary image/icon assets were inventoried by path and size; they contain no executable business logic.

## Baseline commands

```powershell
git status --short
git branch --show-current
git log -1 --oneline
git checkout -b feat/unified-license-audit
rg --files -g "!bin/**" -g "!obj/**"
Get-ChildItem -Recurse -Force
dotnet --info
dotnet build WinLicApp\WinLicApp.csproj -c Debug --no-restore
MSBuild.exe WinLicApp\WinLicApp.csproj /t:Build /p:Configuration=Debug
MSBuild.exe WinLicApp\WinLicApp.csproj /t:Build /p:Configuration=Release
[System.Management.Automation.Language.Parser]::ParseFile(...)
```

See `baseline-results.md` for outcomes and limitations.

## Discovery conclusion

The baseline is a Windows-license management application, not a multi-product audit application. It contains useful Windows evidence acquisition and bilingual UI assets, but the business logic is duplicated between a large WPF code-behind and a monolithic interactive PowerShell script. It has no shared engine, scanner contract, structured result model, report layer, automated test suite, installer, or verified multi-architecture release design.
