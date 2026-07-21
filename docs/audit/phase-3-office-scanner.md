# Phase 3 — read-only Office license scanner

## Objective and inventory

Phase 3 adds a `net48`, vendor-specific scanner for Office suites, Microsoft 365 Apps, Project, and Visio. The existing Office tab was only a localized “under development” placeholder. The only other Office-related code was a piracy-scan heuristic that labeled external KMS hosts suspicious; it is unsuitable for license status, was not reused, and remains outside this scanner. No existing OSPP, vNext, Click-to-Run status parser, Project/Visio result logic, or Office activation UI existed.

## Architecture

`WinLic.Scanners.Office` separates installation detection/tool location, official-tool acquisition, string-only parsers, conservative classification, and result mapping. `OfficeLicenseScanner` implements `ILicenseScanner` with stable ID `microsoft.office` and can return multiple results. Core and the Windows scanner were not modified.

Click-to-Run detection reads both Registry views at `HKLM\SOFTWARE\Microsoft\Office\ClickToRun\Configuration`, including `ProductReleaseIds`, installation path, version, and platform. Local/MSI-compatible detection checks Office14/15/16 and `root\Office16` under both Program Files roots for Word, Project, and Visio executables. Uninstall metadata is not license evidence. Language packs, proofing tools, Teams, and OneDrive identifiers are excluded.

`OfficeToolLocator` checks installation roots deterministically for `OSPP.VBS` and `vnextdiag.ps1`; x86 and x64 roots remain distinct. Multiple locations are ordered and de-duplicated. Discovery never assumes Office 2019 uses `Office19` or that Office bitness equals OS bitness.

## Read-only boundary

| Operation | Office scanner |
|---|---|
| Detect Office installation | Allowed |
| Read Click-to-Run configuration | Allowed |
| Read MSI/local metadata | Allowed |
| ospp `/dstatus` | Allowed |
| ospp `/dstatusall` | Allowed |
| Official vNext diagnostic `-action list` | Allowed |
| Install/remove Office product key | Prohibited |
| Activate Office | Prohibited |
| Set/remove KMS host or port | Prohibited |
| Rearm Office | Prohibited |
| Install license file | Prohibited |
| Modify Registry licensing data | Prohibited |
| Read credentials/tokens | Prohibited |

Providers use `IProcessRunner`, fixed options, finite 30-second timeouts, hidden windows, captured output, cancellation, and no elevation. PowerShell uses `-NoLogo -NoProfile -NonInteractive -File <installed official script> -action list`; it does not bypass or alter execution policy.

## Parsing, identity, and mapping

OSPP and vNext have separate parsers. English/Vietnamese labels, CRLF/LF, Unicode, multiple blocks, missing fields, and malformed/unknown output are fixture-tested. Unknown output yields no positive/negative conclusion. Product family is kept separately from localized display text, preserving Office suite, Microsoft 365 Apps, Project, Visio, and Access identities.

Explicit licensed/unlicensed, grace/notification, trial/evaluation, expired, sign-in, and online-refresh states map to the corresponding Core status. Installation-only Microsoft 365 Apps maps to `NeedsOnlineVerification`; it never maps to `Unlicensed`. KMS client is a legitimate `Volume_KMSCLIENT` type and receives only a periodic-renewal explanation, never a piracy label.

OSPP KMS/grace duration is an activation-renewal interval, not an organizational contract end date. vNext expiration is recorded as cached-entitlement expiration and not claimed as subscription termination without stronger evidence.

## Confidence, privacy, and isolation

Official diagnostic evidence maps to Medium confidence unless corroborated; installation-only evidence is Low. No executable/Registry presence alone receives High confidence.

Full product-key patterns and email addresses are masked before parser/result boundaries. Only a masked final-five key representation is returned. Raw stdout is not placed in `LicenseResult`. Tenant IDs, tokens, credentials, cookies, and authentication-cache files are not collected. Errors are reduced to source warnings without stack traces.

OSPP and vNext acquisition failures are independent. Completed product results remain available when another source is unavailable. Cancellation is checked before detection, each provider, and each result mapping step.

## WPF integration

The Office tab now provides Scan and Scan Again actions plus localized progress text. It calls an Office-only `AuditOrchestrator` created by `ApplicationCompositionRoot`, runs the bounded scanner off the UI thread, and renders each returned product separately in the existing log. It adds no activation button, user-supplied command field, UAC request, or visible console.

## Tests and limitations

The dedicated test assembly covers English/Vietnamese OSPP and vNext fixtures, multiple products, Project/Visio separation, status/channel classification, KMS neutrality, account/key masking, missing tools, fixed OSPP allowlist, cancellation, non-null results, and Microsoft 365 online-verification behavior. Standard tests never call installed Office tools.

MSI detection is conservative filesystem evidence rather than a complete Windows Installer product inventory. Exact SKU-to-marketing-name coverage, all localized diagnostics, Microsoft 365 versions before vNext script inclusion, Windows 7/8, x86 runtime, and ARM64 remain **NEEDS VERIFICATION**. Phase 4 may add unified multi-scanner audit/reporting only after this scanner is reviewed; it must preserve the separate official-tool boundaries.

## Build and smoke results

Verification on the current Windows 10 22H2 x64 development machine used .NET SDK 10.0.302. Restore passed. Debug and Release solution builds passed with zero errors and only the eight pre-existing nullable warnings in `MainWindow.xaml.cs`. Debug and Release tests each passed: Core 60/60, Windows scanner 52/52, Office scanner 44/44 (156 total, zero failed/skipped).

The default Debug WPF output was locked by a pre-existing `WinLicApp.exe` running at a different integrity boundary, so the identical Debug source was built and tested through an isolated `OutDir`; default Release output built normally. This environment-only workaround did not modify project settings.

No Office, Project, Visio, Click-to-Run configuration, or official diagnostic tool was detected on this machine. The Office-tab smoke test therefore exercised the not-installed path: startup PASS, scan PASS in 0.13 seconds, localized no-products message displayed, UI responsive, scan button re-enabled, no UAC, no console window, no full key, and no account identifier. This is not a machine-validation claim for an installed Office product.
