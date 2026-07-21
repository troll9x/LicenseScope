# Phase 5 Autodesk licensing research

Accessed: 2026-07-21. Scope: Windows desktop products using Autodesk Licensing Service, principally 2020 and later; older releases are limited.

## Official sources reviewed

- Autodesk Support, "License server information is not showing correctly in Manage License window": documents `AdskLicensingInstHelper list`, registration fields, and method codes 0 Unknown, 1 Network, 2 Standalone, 3 deployment Standalone, 4 User Licensing.
- Autodesk Support, "General information about Autodesk Licensing Components for the named-user license": named-user access depends on Identity Manager, Licensing Service, and SSO.
- Autodesk Installation Help, "Plan your software license": distinguishes named-user account authentication, device-bound standalone licensing, and Network License Manager authentication.
- Autodesk Installation Help, "About Network License Management": checkout occurs when a product requests a license from NLM; configuration alone is not checkout evidence.
- Autodesk Support, "How to check the status of Autodesk Desktop Licensing Service": the service is used by version 2020 or later.

## Evidence meaning and limits

`AdskLicensingInstHelper list` is the only helper operation allowed by WinLic. It enumerates registered features and configured licensing methods. Product/feature codes and selected/default versions support matching. `supported_lic_methods` describes capability, not current entitlement. Server type/presence describes configuration only; addresses are discarded. Serial fields are ignored.

Registration does not prove seat assignment, subscription validity, sign-in, Flex token balance, network checkout, standalone activation, trial status, or launch ability. A running service is operational evidence only. Named User versus Flex cannot be distinguished locally: **NEEDS VERIFICATION**. Identity/SSO state without reading account/cache data is **NEEDS VERIFICATION**.

Network checkout is initiated by a product. WinLic neither launches products nor probes servers, so Network and User Licensing return `NeedsOnlineVerification`. Standalone legacy returns `Unknown`. No researched read-only source was strong enough to produce `Licensed`, `Unlicensed`, `Trial`, or `Expired` in Phase 5.

Allowed reads: uninstall metadata, file existence/version, service status, exact helper argument `list`. Prohibited: configuration changes, product registration/removal/reset, service mutation, login/cache/token reads, file/Registry writes, account APIs, server probes, and product launch. False positives are controlled by excluding shared components and conservative status mapping. False negatives remain possible for legacy, per-user, suite, damaged, or unfamiliar installations.
