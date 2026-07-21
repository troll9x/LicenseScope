# Phase 6 — read-only Adobe scanner

## Objective and baseline

Phase 5 commit `2c52c06` was verified on a clean `feat/unified-license-audit` tree. Debug baseline build passed and all 208 existing tests passed. Phase 6 adds scanner ID `adobe.desktop` as the fourth production scanner.

## Design

The net48 project separates Registry installation detection, product catalog, OperatingConfigs detection, trusted toolkit location, process acquisition, text parsing, product matching, classification, service evidence, and result mapping. GUI and CLI use the common factory in deterministic order Windows, Office, Autodesk, Adobe; JSON/CSV/HTML use the existing sanitized reporting pipeline.

HKLM x86/x64 uninstall metadata recognizes Photoshop, Illustrator, Premiere Pro, After Effects, InDesign, Acrobat Pro/Standard, Lightroom/Classic, Audition, Animate, Dreamweaver, Media Encoder, Bridge, Character Animator, and Fresco. Creative Cloud Desktop, Genuine/Desktop/Update services, IPC/CCX/CoreSync, Camera Raw, UXP/CEP, identity, installer, language/help/library/licensing components are excluded. Catalog entries are provenance-limited to Adobe toolkit AppId examples and fixture-validated mappings; unknown identities remain conservative.

OperatingConfigs handling reads existence and count only. No filename, OperatingConfig, ASNP, certificate, signature, organization value, account, serial, token, or raw toolkit output is retained.

## Toolkit and read-only boundary

The toolkit is not bundled or downloaded. The only trusted location is `<AppBase>\tools\adobe\adobe-licensing-toolkit.exe`; filename and canonical containment are enforced. The provider directly runs only `--licenseInformation`, hidden, redirected, with 30-second timeout/cancellation, no shell and no elevation request.

| Operation | Adobe scanner |
|---|---|
| Detect installed Adobe applications | Allowed |
| Read uninstall/install metadata | Allowed |
| Read Adobe service status | Allowed |
| Detect OperatingConfigs existence/count | Allowed |
| Run toolkit `--licenseInformation` | Allowed |
| Run toolkit `-l` | Allowed equivalent, not used |
| Activate/deactivate Shared Device license | Prohibited |
| View verbose ASNP activation data | Prohibited |
| Read/decode login tokens | Prohibited |
| Read raw ASNP/OperatingConfig | Prohibited |
| Sign user in or launch an application | Prohibited |
| Modify Registry/license files | Prohibited |
| Download or bundle toolkit | Prohibited in Phase 6 |
| Contact Adobe Admin Console/API | Prohibited |

## Classification and expiry

Installation-only Named User products return `NeedsOnlineVerification`. A recognized Shared Device or FRL record with exact AppId and unambiguous future LicenseExpiry can return `Licensed`; a past LicenseExpiry returns `Expired`. A stale CacheExpiry with a future LicenseExpiry returns `NeedsOnlineVerification`. Named User Education Lab remains online-verification dependent. Unknown mode/date and legacy evidence return `Unknown`; missing evidence never returns `Unlicensed`. Adobe Genuine Service is infrastructure evidence only.

Expected environmental errors are structured warnings. Cancellation propagates to the shared process runner. Orphan toolkit records do not create installed-product rows. Results are deterministic and per application.

## Compatibility matrix

| Family | Implemented | Fixture-tested | Machine-tested | Status |
|---|---:|---:|---:|---|
| Named User | Detection/policy | Yes | Current machine path | Online verification |
| Shared Device | Config/toolkit support | Yes | Toolkit absent | Conditional |
| Feature Restricted | Known modes | Yes | No | NEEDS VERIFICATION |
| Serial/perpetual legacy | Install detection | Policy | No | Unknown |
| Creative Cloud current | Install detection | Yes | Current machine path | Conservative |
| Acrobat perpetual | Install detection | Limited | No | NEEDS VERIFICATION |
| Adobe 2019 and older | Install detection | Limited | No | NEEDS VERIFICATION |

Claims are limited to net48/AnyCPU build, fixture testing, current-machine installed/no-install behavior, and toolkit support when an administrator supplies the official binary in the trusted path. No Windows 7/8, ARM64, native architecture, Admin Console entitlement, or universal-version claim is made. Phase 7 recommendation: implement SketchUp with independently documented Classic/subscription evidence and the same conservative privacy boundary.
