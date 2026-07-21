# Phase 6 Adobe licensing research

Accessed 2026-07-21. Only Adobe Help Center and Enterprise documentation were used.

## Licensing models

Adobe documents Named User Licensing as user-associated licensing that requires the user to sign in; installed applications and local Creative Cloud infrastructure do not prove a current assignment. WinLic therefore returns `NeedsOnlineVerification` unless an official signed-out fact becomes available. No login cache, IMS data, email, or token is read.

Shared Device Licensing associates licensing with a lab/shared machine, while users can still be required to sign in and remain subject to organizational access policy. Adobe states that `%ProgramData%\Adobe\OperatingConfigs` exists for Shared Device installations. WinLic checks only directory existence and top-level count; it never reads filenames or contents. Presence alone is not proof of a valid license.

Feature Restricted Licensing supports restricted environments. Adobe documents deployment identifiers including `FRL_CONNECTED` for specific products. Complete semantics and expiration behavior across every FRL variant are **NEEDS VERIFICATION**. Serial/perpetual legacy activation has no sufficiently authoritative read-only local source in scope and remains `Unknown`.

Adobe Genuine Service performs genuine-software/license-integrity checks and can notify users, but service state does not expose an authoritative per-product result. Running never means `Licensed`; missing never means `Unlicensed`.

## Official Licensing Toolkit

Adobe documents `adobe-licensing-toolkit.exe --licenseInformation` (`-l`) to view installed license information. Sample fields are NpdId, AppId, DeploymentMode, CacheExpiry, LicenseId, and LicenseExpiry. WinLic retains only AppId, mode, parsed expiry values, and booleans indicating identifier presence. Raw identifiers and stdout are discarded.

`LicenseExpiry` is used only when its timezone/date format parses unambiguously. Future Shared Device or recognized FRL expiry can support `Licensed`; past expiry supports `Expired`. `CacheExpiry` is separate: stale cache with future license expiry produces `NeedsOnlineVerification`, not `Expired`. Named timezone strings that cannot be mapped reliably are rejected rather than guessed.

The toolkit also documents activation/deactivation and verbose package/ASNP operations. Those operations are prohibited. The public provider exposes only `ReadLicenseInformationAsync`; its internal argument is fixed to `--licenseInformation`.

WinLic does not download, bundle, redistribute, copy, or update the toolkit. Redistribution permission is **NEEDS VERIFICATION**. An administrator may provision it later at the application-managed trusted path `tools\adobe\adobe-licensing-toolkit.exe`; no GUI/CLI path input exists.

## Limits and risk

Offline verification cannot establish current Named User assignment, sign-in, cloud services, or Admin Console policy. Shared Device users may still need identity/access authorization. Product catalog and localized install metadata can cause false negatives; strict infrastructure filtering and exact AppId matching reduce false positives. Toolkit-absent, malformed, ambiguous-date, and installation-only cases never become `Unlicensed`.
