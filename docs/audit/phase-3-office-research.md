# Phase 3 Office licensing research

Research date: 2026-07-21. Only Microsoft documentation and the installed-product tool locations described by Microsoft were accepted as authority.

## Official sources reviewed

- [Check the license and activation status for Microsoft 365 Apps](https://learn.microsoft.com/en-us/microsoft-365-apps/licensing-activation/vnextdiag), last-updated date shown by Microsoft: 2024-07-22.
- [Tools to manage volume activation of Office](https://learn.microsoft.com/en-us/office/volume-license-activation/tools-to-manage-volume-activation-of-office), accessed 2026-07-21.
- [Troubleshoot volume activation of Office](https://learn.microsoft.com/en-us/office/volume-license-activation/troubleshoot-volume-activation-of-office), accessed 2026-07-21.
- [Sign in to Microsoft 365](https://support.microsoft.com/en-us/office/account-management/lc-account/sign-in-to-microsoft-365), accessed 2026-07-21.

## Verified scope

Microsoft states that Microsoft 365 Apps moved away from the Office Software Protection Platform starting with version 1910. `ospp.vbs` must therefore not be used to conclude modern subscription status. `vnextdiag.ps1` is included starting with Microsoft 365 Apps version 2104; its read-only action is `-action list`. Subscription Project and Visio use the new method starting with version 1910.

Volume-licensed Office, Project, and Visio continue to use OSPP. Microsoft documents `/dstatus` as displaying installed-key license information and `/dstatusall` as displaying all installed licenses. These are the only OSPP actions allowed by this scanner.

| Tool | Applies to | Allowed read | Output | Offline limitation |
|---|---|---|---|---|
| `ospp.vbs` | Volume/perpetual Office including LTSC, volume Project and Visio | `/dstatus`, `/dstatusall` | Localized text with product/license fields | Does not establish current Microsoft 365 subscription entitlement |
| `vnextdiag.ps1` | Microsoft 365 Apps and new-method subscription Project/Visio | `-action list` | Product, license type/state, account and tenant fields | Cached state can require sign-in or online verification |

## Prohibited actions

OSPP activation, product-key install/removal, rearm, license-file install, and KMS host/port changes are administrative mutations and prohibited. The vNext `remove` action resets activation and is prohibited. The scanner does not accept user-provided options.

The application does not change PowerShell execution policy, download scripts, inspect raw license-cache files, or read credentials, tokens, cookies, browser sessions, or Credential Manager secrets.

## Status boundaries

Official-tool `Licensed` is usable license evidence. Explicit `Unlicensed` is mapped only when returned by the applicable official diagnostic. A missing tool, denied source, missing sign-in, stale cache, or offline verification requirement is never converted to `Unlicensed`.

Microsoft documents email and tenant ID in vNext output. WinLic masks email before evidence mapping and does not retain tenant identifiers. Determining whether every historical/localized output distinguishes trial expiration from subscription expiration remains **NEEDS VERIFICATION**.

## Version matrix

| Family | Implementation state |
|---|---|
| Office volume/perpetual using OSPP | Implemented; fixture-tested |
| Office LTSC/2019/2021/2024 product strings | Parser/classifier implemented; machine behavior NEEDS VERIFICATION |
| Microsoft 365 Apps version 2104+ with vNext diagnostic | Implemented; fixture-tested |
| Microsoft 365 Apps 1910–2103 | New activation method documented; diagnostic availability NEEDS VERIFICATION |
| Subscription Project/Visio version 1910+ | Implemented through vNext fixtures; machine behavior NEEDS VERIFICATION |
| Office 2010/2013/2016 MSI | Installation/path detection present; exact localized OSPP behavior NEEDS VERIFICATION |
