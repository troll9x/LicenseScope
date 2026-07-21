# Current architecture

## Runtime components

### WPF GUI

`WinLicApp` is a single SDK-style .NET Framework 4.8 WPF project. `App.xaml` starts `MainWindow` directly. `App.xaml.cs` checks the .NET Framework `Release` registry value and, when it believes 4.8 is absent, offers to open a Microsoft download page before shutting down. This check cannot replace an installer prerequisite bootstrapper because the CLR/WPF application must load before `OnStartup` can execute.

The manifest uses `asInvoker`; elevation is requested later by relaunching the executable with the `runas` verb. It declares Windows 10 and Windows 8.1 compatibility GUIDs only. It contains no explicit long-path, per-monitor DPI, Windows 7, or Windows 8 declaration.

### PowerShell CLI

`WinLicPS/WinLicManager.ps1` is a separate interactive PowerShell implementation with eight menu options. It does not consume GUI assemblies and the GUI does not consume it. It is not the requested single `audit --all` command and does not emit a common structured result contract.

### Configuration and localization

- `Localization.cs` is a static EN/VI string dictionary used by the WPF application.
- `AppSettings.cs` parses and writes `settings.ini`, merges embedded fallback lists with default and user lists, and can download updated defaults from GitHub.
- `WinLicPS/settings.ini` contains both default and user blocks; `settings.default.ini` is the downloadable default block.
- Settings are written beside the executable. This may fail when installed under `Program Files` for a non-admin user and must be redesigned before an installer is introduced.

## Dependency direction at baseline

```text
WPF views
  -> WPF code-behind
     -> System.Management / Registry / Process / networking / filesystem
     -> Localization
     -> AppSettings

PowerShell menu
  -> independent WMI / Registry / slmgr / networking / filesystem logic
```

There is no domain/core boundary. There are no interfaces, dependency injection, scanner discovery, models, repositories, report writers, or platform abstraction layers.

## Concentration of logic

`MainWindow.xaml.cs` is approximately 118 KB and owns UI state plus Windows licensing operations, WMI/registry queries, `slmgr` execution/parsing, product-key handling, piracy heuristics, networking, elevation, shutdown/restart, and KMS/channel operations. `MainWindow.xaml` is approximately 61 KB and contains the eight-option workflow and inline confirmation panels. `WinLicManager.ps1` is approximately 156 KB and duplicates much of this behavior.

Only limited async work exists (notably KMS network checks and settings/update calls). Major WMI, registry, process, event-log, and audit work is invoked from UI event handlers, so responsiveness for a future all-product scan is not established.

## Reusable candidates

- Read-only WMI queries against `SoftwareLicensingProduct` and `SoftwareLicensingService`, after extraction and test coverage.
- Read-only SPP registry access and partial-key/channel/grace evidence collection.
- `slmgr /dli` and `/dlv` execution concepts, but parsers are locale-sensitive and require fixtures/safer evidence mapping.
- Product-key masking concept and Windows product-key decoding, after security review.
- EN/VI terminology and selected WPF styles/resources.
- INI parsing/merge behavior for audit signatures, after separating data from licensing conclusions.
- Exception-tolerant patterns around individual evidence sources.

License-changing paths (`/ipk`, `/ato`, `/upk`, `/cpky`, `/rearm`, `/skms`, `/ckms`, restart) are not reusable in the default audit engine. If retained at all, they belong in a separately gated administration feature or a separate build.

## Proposed target architecture based on the code

```text
WinLic.Core
  Contracts, Models, SystemContext, AuditOrchestrator, security masking
       ^
       |
WinLic.Scanners.Windows    WinLic.Scanners.Office    future vendor scanners
       ^                         ^
       +------------+------------+
                    |
              WinLic.Reporting
               ^            ^
               |            |
         WinLic.App.Wpf   WinLic.Cli
```

Recommended boundaries:

1. `WinLic.Core` must not reference WPF, scanner implementations, reporting formats, or process/UI types.
2. Each scanner returns structured `LicenseResult` values and evidence. Source failures become scanner/evidence errors rather than process-wide failures.
3. GUI and CLI compose the same scanner collection and orchestrator.
4. Windows evidence collection is split into injectable WMI, registry, command, filesystem, and clock abstractions to permit fixture-based tests.
5. Reporting consumes already-masked audit models. Raw secrets must not enter default logs/reports.
6. Administration commands are outside scanner contracts and outside `audit --all`.
7. Legacy and modern WPF hosts may share compatible source/core contracts but require target-specific build projects if one target cannot cover the OS matrix safely.

This architecture is a proposal for Phase 1, not implemented in Phase 0.
