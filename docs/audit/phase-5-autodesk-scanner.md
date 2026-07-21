# Phase 5 — read-only Autodesk scanner

## Objective and baseline

Phase 4 was verified at `4f3ae48` on `feat/unified-license-audit`: clean tree, Debug build pass, 184/184 existing tests pass. Phase 5 adds `autodesk.desktop` without changing licensing state.

## Architecture and integration

The net48 scanner separates Registry detection, official-helper location, process acquisition, JSON parsing, deterministic matching, classification, and result creation. `ProductionScannerFactory` orders Windows, Office, Autodesk. The existing orchestrator isolates failures and supplies GUI/CLI progress; generic reporting exports Autodesk results to JSON, CSV, HTML.

The locator uses `%CommonProgramFiles(x86)%\Autodesk Shared\AdskLicensing\Current\helper\AdskLicensingInstHelper.exe`, reads existence/version, and neither searches drives nor accepts a caller path. Acquisition directly invokes the internal constant `list`, hidden, redirected, with a 30-second timeout, cancellation, no shell, no UAC request.

Detection reads HKLM 32/64 uninstall metadata and excludes Access, Desktop App, Licensing, Genuine Service, Material/Language/Content packs, Identity Manager, and SSO components. Matching uses product/feature identity and a unique release-year fallback; ambiguity remains unmatched.

## Status, privacy, and resilience

Method 4 is `UserLicensing`, method 1 `Network`; both yield `NeedsOnlineVerification`. Methods 2/3 yield `Unknown`. Missing/malformed evidence never becomes `Unlicensed`. Installation, registration, and running service never become `Licensed`. Stopped/missing service adds a warning only.

Serials, accounts, raw stdout, and server values are not retained. No token, cache, cookie, email, or profile path is collected. Helper absence/timeout/nonzero/malformed output and Registry/service errors become structured warnings; cancellation propagates.

| Operation | Autodesk scanner |
|---|---|
| Detect installed products | Allowed |
| Read licensing service status | Allowed |
| Run `AdskLicensingInstHelper list` | Allowed |
| Read registration/method metadata | Allowed |
| Read legacy configuration | Allowed, limited |
| Change licensing method | Prohibited |
| Register/deregister/reset | Prohibited |
| Start/stop service | Prohibited |
| Read authentication token/cache | Prohibited |
| Contact Autodesk Account API | Prohibited |
| Probe license server | Prohibited |
| Launch product for checkout | Prohibited |
| Modify Registry/licensing files | Prohibited |

## Tests and version matrix

Fixtures cover noisy/malformed/Unicode/multiple/duplicate output, privacy, method policy, fixed allowlist, failures, matching, multiple/orphan/unregistered products, service non-inference, empty machine, and cancellation. Real Autodesk integration is disabled by default.

| Family | Implemented | Fixture-tested | Machine-tested | Status |
|---|---:|---:|---:|---|
| Autodesk 2020+ service | Yes | Yes | Current-machine path | Conservative |
| Autodesk 2017–2019 | Install detection only | Limited | No | NEEDS VERIFICATION |
| Autodesk 2016 and older | Install detection only | No | No | NEEDS VERIFICATION |
| Named User | Method detection | Yes | No | Online verification needed |
| Flex | Not distinguishable | Policy | No | NEEDS VERIFICATION |
| Network | Configuration only | Yes | No probe | Online verification needed |
| Standalone legacy | Method only | Yes | No | Unknown |

Limits: incomplete product catalog/suite matching; no entitlement, sign-in, Flex balance, checkout, trial/expiry, or legacy activation verification. Claim is net48/AnyCPU build and fixture testing only—not Windows 7/8, ARM64, or native-architecture certification. Phase 6 recommendation: apply the same documented conservative boundary to Adobe.
