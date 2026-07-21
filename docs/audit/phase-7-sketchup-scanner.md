# Phase 7 — read-only SketchUp scanner

## Objective and baseline

Phase 6 HEAD `6f0b648` and content-clean working tree were verified. Debug baseline passed with all 239 existing tests. Phase 7 adds `trimble.sketchup` as the fifth production scanner.

## Architecture and evidence

The net48 project separates Registry installation detection, subscription metadata, Classic/network metadata, classification and result mapping. HKLM Registry32/64 recognizes SketchUp Desktop releases 2017–2026 where metadata contains a resolvable year. Multiple releases produce separate results. LayOut and Style Builder remain component evidence on the parent result; Viewer, Importer, SDK, language packs, helpers, Trimble Connect, V-Ray and Enscape are excluded.

Subscription detection checks version-profile and `login_session.dat` existence plus last-write metadata only. Contents and raw user path are never read or retained. Session presence and timestamps never create `Licensed` or `Expired`; subscription returns `NeedsOnlineVerification`.

Classic detection checks known ProgramData artifact existence only. Neither `activation_info.txt` nor `server.dat` is opened. Classic single-user evidence returns `Unknown`; network evidence returns `NeedsOnlineVerification`. No server address, seat, serial, authorization code, checkout or check-in is accessed.

| Operation | SketchUp scanner |
|---|---|
| Detect installed SketchUp versions | Allowed |
| Read uninstall/install metadata | Allowed |
| Read executable version | Allowed where metadata is available |
| Check session artifact existence | Allowed, metadata only |
| Read session contents | Prohibited |
| Check Classic artifact existence | Allowed, metadata only |
| Read serial/authorization code | Prohibited |
| Read network server details | Prohibited |
| Launch SketchUp/LayOut/browser | Prohibited |
| Sign in/sign out or call Trimble API | Prohibited |
| Authorize/remove Classic license | Prohibited |
| Checkout/check-in or probe server | Prohibited |
| Modify/delete license/session files | Prohibited |

Environmental access errors become sanitized warnings; cancellation is checked before detection and each evidence provider. The existing orchestrator isolates unexpected errors. GUI/CLI use the common deterministic order Windows, Office, Autodesk, Adobe, SketchUp, and generic JSON/CSV/HTML reporting requires no new writer.

## Compatibility matrix

| Family | Implemented | Fixture-tested | Machine-tested | Status |
|---|---:|---:|---:|---|
| Subscription 2020–2026 | Metadata/policy | Yes | Current-machine path | Online verification |
| Classic 2017–2019 | Artifact/policy | Yes | No | Unknown |
| Classic network | Artifact/policy | Yes | No probe | Online verification |
| Education/lab | No authoritative evidence | Policy | No | NEEDS VERIFICATION |
| Trial | No authoritative evidence | Policy | No | NEEDS VERIFICATION |
| SketchUp 2016 and older | No | No | No | Unsupported/unknown |

Claims are limited to net48/AnyCPU build, fixtures, installed/no-install machine path and conservative classification. No subscription assignment, device count, Classic validity, network availability, trial expiry, extension/V-Ray/Enscape, Windows 7/8, ARM64 or native-architecture claim is made. Phase 8 recommendation: compatibility and multi-target research without expanding vendor scanners.
