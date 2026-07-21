# Phase 2 — Read-only Windows license scanner

## Objective

Phase 2 adds a production `microsoft.windows` scanner that returns a structured Windows `LicenseResult` through the Phase 1 orchestrator. It migrates only the Option 1 inspection entry point. Existing key installation/removal, activation, rearm, channel, and KMS administration paths remain unchanged and outside the scanner.

## Existing logic inspected and migration inventory

The complete Windows paths in `MainWindow.xaml.cs`, `AppSettings.cs`, `Localization.cs`, `MainWindow.xaml`, `WinLicManager.ps1`, and both INI files were reviewed.

| Existing concern | Phase 2 disposition |
|---|---|
| `SoftwareLicensingProduct` and `SoftwareLicensingService` SELECT queries | Migrated behind read-only WMI service |
| SPP `BackupProductKeyDefault` read | Supporting evidence only; masked immediately |
| OA3 BIOS key read | Supporting evidence only; masked immediately |
| `slmgr /xpr`, `/dlv` | Fixed secondary read-only commands |
| Status/channel/partial-key logic | Reimplemented as typed selection/classification |
| Generic/GVLK lists in mutable `AppSettings` | Not used; scanner owns a small compiled suffix catalog |
| Digital-license heuristic | Conservative multi-signal, medium/high evidence explanation |
| KMS discovery | Reported neutrally; never classified as piracy |
| Option 5 piracy heuristics | Not migrated; unsuitable for license-status conclusion |
| Product-key decode/full-key display | Not migrated |
| `/ipk`, `/upk`, `/cpky`, `/rearm`, `/ato`, `/skms`, `/ckms` | Prohibited from scanner; retained only in legacy administration UI |

The implementation separates evidence acquisition, localized string parsing, candidate selection, classification, result mapping, and WPF presentation.

## Read-only boundary

| Operation | Audit scanner |
|---|---|
| WMI SELECT | Allowed |
| Registry read | Allowed |
| `slmgr /xpr` | Allowed |
| `slmgr /dlv` | Allowed |
| Install key | Prohibited |
| Remove key | Prohibited |
| Activate | Prohibited |
| Rearm | Prohibited |
| Set/Clear KMS | Prohibited |
| Registry licensing write | Prohibited |
| WMI licensing method invocation | Prohibited |

`WindowsWmiQueryService` accepts one query beginning with SELECT and exposes only dictionaries. `WindowsRegistryReader` opens HKLM keys with `writable:false` and supports explicit registry views. `WindowsSlmgrEvidenceProvider` has a hard allowlist containing only `/xpr` and `/dlv`; callers cannot provide arbitrary options.

## Project structure and dependencies

```text
WinLic.Core
    ^
    |
WinLic.Scanners.Windows <--- WinLic.Scanners.Windows.Tests
    ^
    |
WinLicApp
```

The scanner project targets `net48` and references Core plus the .NET Framework `System.Management` assembly. Core remains vendor/UI independent.

## Scanner contract

`WindowsLicenseScanner` implements `ILicenseScanner` with stable ID `microsoft.windows` and vendor `Microsoft`. Applicability requires a Windows-like OS name or a populated Windows directory. A scan always returns a non-null list containing one installed-Windows result; ordinary acquisition loss becomes warnings and `Unknown`, not an exception or `Unlicensed` assumption.

## Evidence hierarchy

1. Windows `SoftwareLicensingProduct` typed WMI rows.
2. `SoftwareLicensingService.OA3xOriginalProductKey`, immediately masked.
3. `/xpr` and `/dlv` secondary evidence.
4. SPP registry backup key as low-confidence supporting evidence, immediately masked.

The WMI product query filters the Windows Application ID `55c92734-d682-4d71-983e-d6ec3f16059f`. Fields include identity, description/channel, numeric status, partial key, grace minutes, evaluation date, and KMS configuration. The service query reads OA3 firmware-key presence. Registry reads `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform\BackupProductKeyDefault` in the 64-bit view.

## Candidate selection

Selection excludes records that neither have the Windows Application ID nor a Windows product name. Candidates receive deterministic scores: Licensed outranks grace, grace outranks unlicensed; a five-character partial key, correct Application ID, and description add secondary weight. Ties are ordered by product identity and produce an ambiguity warning. The first WMI row is never selected implicitly.

## Status mapping

| WMI value | Core status |
|---:|---|
| 0 | Unlicensed |
| 1 | Licensed |
| 2 OOBGrace | GracePeriod |
| 3 OOTGrace | GracePeriod |
| 4 NonGenuineGrace | GracePeriod plus validation warning; no piracy conclusion |
| 5 Notification | Unlicensed only when `/xpr` corroborates; otherwise Unknown |
| 6 ExtendedGrace | GracePeriod |
| other/missing | Unknown |

A reliably parsed expired evaluation/KMS date can change the result to Expired. Zero grace minutes alone does not.

## Channel and activation classification

Channel parsing is case-insensitive and recognizes Retail, OEM_DM, OEM_SLP, OEM_COA, Volume_MAK, Volume_KMSCLIENT, Volume_KMS, Evaluation, and Unknown. Status and channel remain independent.

DigitalLicense is inferred only for a licensed Retail record with a reviewed generic installation-key suffix and corroborating permanent `/xpr` evidence. The warning explicitly says this is an inference, not a manufacturer API assertion. A generic key, backup mismatch, or Retail channel alone is insufficient.

KMS is treated as legitimate volume licensing. A licensed KMS client remains Licensed and receives a periodic-renewal warning. Presence or absence of a KMS hostname never creates a piracy conclusion. OEM firmware evidence can identify an OEM method but never overrides an unlicensed active record or proves edition compatibility.

## Confidence

- High: typed WMI status plus parsed secondary evidence, without ambiguous selection.
- Medium: typed WMI status when secondary evidence is unavailable, or ambiguity reduces a corroborated result.
- Low: parsed secondary evidence without a selected WMI product.
- None: no usable status evidence.

## Localization and parsers

String-only parsers support initial English and Vietnamese permanent, notification/unlicensed, expiration, description, status, partial-key, and KMS labels. They accept CRLF/LF and culture-aware dates. Unknown languages, malformed lines, and empty output return unparsed/Unknown without throwing. Typed WMI remains primary because `slmgr` output is localized. Unparsed raw output is not copied into user reports or results.

## Error, privacy, and security behavior

WMI, OA3, registry, and slmgr failures are isolated per source. Warnings contain sanitized source/type information, not stack traces. Access denied is not Unlicensed. Timeout and start failures are structured. Cancellation propagates to the orchestrator.

Full OA3 and registry keys are masked at acquisition. WMI partial keys are normalized to the last five characters; final results use `XXXXX-XXXXX-XXXXX-XXXXX-SSSSS`. Exact KMS hostnames are not placed in result evidence. No credentials, user hives, telemetry, upload, or network service are used.

## WPF integration

`ApplicationCompositionRoot` constructs the scanner, process runner, clock, context provider, and orchestrator manually. The existing Option 1 XAML button now invokes `BtnWindowsAudit_Click`; its old handler is no longer wired. The new event handler disables the button, runs the bounded WMI/process audit on a background task because .NET Framework WMI has no asynchronous query API, awaits without blocking the UI, renders structured fields/evidence/warnings, and re-enables the button. It does not request UAC.

Existing administration buttons and confirmation panels are unchanged. The old Option 1 body remains temporarily as inactive migration reference because broad removal from the monolithic file is outside this phase.

## Tests

The dedicated MSTest project uses typed fixtures and fake collectors/runners. It covers candidate filtering/ranking/ambiguity, all numeric WMI states, channel cases, digital-license corroboration/exclusions, legitimate and expired KMS cases, OEM non-override, English/Vietnamese and malformed parser fixtures, masking, scanner cancellation/failure behavior, and a command allowlist test proving only `/xpr` and `/dlv` are accepted. No test invokes real WMI, registry licensing, slmgr, KMS, or activation.

## Build and verification results

Verification was performed on Windows 10 22H2 (build 19045), x64 host, with .NET SDK 10.0.302 and the .NET Framework 4.8 targets.

| Gate | Result |
|---|---|
| `dotnet restore WinLic.sln` | PASS; five projects restored/up to date |
| Solution Debug build | PASS; 0 errors, 8 pre-existing nullable warnings in `MainWindow.xaml.cs` |
| Solution Release build | PASS; 0 errors, the same 8 pre-existing warnings |
| Core tests, Debug | PASS; 60/60 |
| Windows scanner tests, Debug | PASS; 52/52 |
| Core tests, Release | PASS; 60/60 |
| Windows scanner tests, Release | PASS; 52/52 |
| PowerShell parser | PASS; 0 parse errors in `WinLicManager.ps1` |
| Prohibited command static scan | PASS; 0 matches in Core, scanner, and tests |
| Registry/WMI write API scan | PASS; 0 matches |
| Empty catch scan | PASS; 0 matches |

## WPF smoke test

The Debug WPF executable was launched at the current user integrity level and Option 1 was invoked through Windows UI Automation. Startup passed. The button was disabled during the scan, the application remained responsive, and the scan completed in 2.75 seconds on this machine. The rendered output contained Product, Status, and Confidence fields and returned the local structured status `Licensed` (`Có bản quyền` in the Vietnamese UI). No UAC request occurred and no full-key pattern appeared in the rendered text. The application was closed after the verification.

This duration and result describe only the current development machine and are not a compatibility or performance guarantee.

## Known limitations and Phase 3 recommendation

Localized parser coverage is initially English/Vietnamese and secondary only. Evaluation WMI date formats vary and may remain unparsed. Digital-license detection is heuristic. This phase does not verify Windows 7/8/8.1, x86, x64-native, or ARM64 behavior and does not claim those targets.

Phase 3 should implement Office/Microsoft 365 research and scanner contracts only after reviewing this Windows scanner and preserving the same read-only, typed-evidence, masking, and fixture-first boundaries.
