# Phase 4 — unified audit, CLI, and reporting

## Objective and baseline

Phase 4 composes the read-only Windows and Office scanners into one audit run, exposes it in WPF and `WinLicAudit.Cli.exe`, and writes sanitized JSON, CSV, and standalone HTML. Baseline was `73c532a`; Debug built at the default output after mapped legacy smoke-test artifacts were safely renamed, and all 156 existing tests passed.

## Architecture and flow

`WinLic.Application` owns deterministic production registration (`microsoft.windows`, then `microsoft.office`) and rejects duplicate IDs. GUI and CLI both call `UnifiedAuditService`; neither reimplements scanners. The flow is registration → `AuditOrchestrator` → `AuditResult` → sanitized snapshot → GUI/console/report writer.

The WPF header contains Scan All, Cancel, JSON/CSV/HTML export, and status filter controls. A progress bar reports real scanner index, and a DataGrid shows each product separately. Cancellation uses `CancellationTokenSource`; completed orchestrator results are preserved. Existing Windows and Office tabs remain.

## Reporting and privacy

`AuditResultSanitizer` creates a new snapshot and never mutates the source. Machine name is excluded by default or pseudonymized when requested. Full keys, email, tenant-like GUIDs, user-profile paths, secret-named evidence, and error values are masked or removed. Raw stdout and stack traces are not reported.

JSON schema `1.0` uses ISO-8601 timestamps, string enums, summary, products, and scanner executions. CSV is UTF-8 with BOM, one product per row, quoted/escaped fields, and prefixes formula-leading cells with `'`. HTML is standalone with inline CSS and entity-escapes all dynamic fields; it has no script, CDN, image, telemetry, or network resource.

Writers validate/normalize paths, create requested directories, refuse overwrite by default, write to a same-directory temporary file, and move it into place only after successful completion. Temporary files are cleaned on failure/cancellation.

## CLI

Required command: `WinLicAudit.Cli.exe audit --all`. Options are `--format json,csv,html`, `--output`, `--include-evidence`, `--no-evidence`, `--include-machine-name`, `--overwrite`, `--quiet`, `--help`, and `--version`. Default report directory is `.\reports` when formats are requested. Scanners run exactly once even for three formats.

Exit codes: 0 clean; 1 unlicensed/expired; 2 incomplete, unknown, sign-in/online verification, or scanner failure; 3 fatal; 4 arguments; 5 report failure; 6 cancelled. Unlicensed takes precedence over incomplete. NotInstalled alone is not failure.

## Verification and limitations

Tests cover sanitization immutability, key/email/path/GUID privacy, summary mapping, stable JSON, CSV BOM/injection, HTML XSS/external-resource defense, safe file behavior, CLI arguments/formats/exit codes/single scan, and scanner-ID uniqueness. Smoke results and final Debug/Release counts are recorded in the Phase 4 result.

Current output remains net48 AnyCPU/default. Windows scan is machine-tested on Windows 10 x64; the Office no-install path is machine-tested. Windows 7/8/ARM, native architecture payloads, installed Office products, installer, and non-Microsoft scanners remain out of scope. Phase 5 should add Autodesk only after review.

## Final gate and smoke evidence

Default-output restore and Debug/Release solution builds passed with zero errors; Debug reported only the eight pre-existing WPF nullable warnings and no new project warnings. Debug and Release tests each passed 184/184: Core 60, Windows 52, Office 44, Reporting 10, CLI 18.

GUI startup and unified scan passed. A cancellation request was observed, progress changed with real scanner execution, the UI remained responsive, and the completed DataGrid contained one licensed Windows result while Office returned zero products. Duration was 1.75 seconds; no UAC or console appeared. Native Save As automation created JSON (1,646 bytes), CSV (337 bytes), and HTML (1,062 bytes) through the three GUI buttons. All three passed unmasked-key, raw-email, and secret scans and were removed afterward.

CLI help exited 0. The real unified command completed in 2.18 seconds with exit 0, ran Windows and Office once, and created JSON (1,641 bytes), CSV (335 bytes), and HTML (1,060 bytes). Windows returned Licensed/OEM_DM; Office returned no installed product. Report scans found no raw email or secret. Full-key regex matches were only explicitly masked `XXXXX-...-final-five` values. Temporary reports were removed.

Static checks found zero prohibited commands, Registry writes, network APIs, shell invocation, empty catches, async-void in CLI/reporting, thread abort/environment exit, and unnecessary Task.Run. The legacy PowerShell script parsed with zero errors.
