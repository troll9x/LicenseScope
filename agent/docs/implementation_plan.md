# WinLic — Expanded Detection + Settings Restructure
## Implementation Plan (approved answers incorporated)

---

## Decisions Confirmed

| Q | Decision |
|---|---|
| GVLK severity | **ERROR** (GVLK + permanent activation = likely piracy; legitimate enterprise has a KMS server configured) |
| Expiry parsing | **WMI** `Win32_SoftwareLicensingProduct.GracePeriodRemaining` (Win10/11 compatible, no subprocess) |
| Ohook/Office | **Deferred** to a future Office Activation Audit |
| TSforge timestamp | **Include** with explicit LOW CONFIDENCE label |
| Rename Option 7 | **No change** yet (will revisit when Office audit is added) |
| GVLK storage | **Full keys** in `settings.ini [GvlkKeys]` section; match by last 5 chars vs WMI `PartialProductKey` |
| Auto-update | **Implemented** via GitHub raw URL download |
| settings.ini | **Restructured** into Default block + User block |

---

## Part A — settings.ini Restructure

### Structure Design

```ini
; ╔══════════════════════════════════════════════════════╗
; ║  WinLic Settings — Default Block                    ║
; ║  Managed automatically. Update via app or script.   ║
; ║  Source: https://raw.githubusercontent.com/         ║
; ║          ardennguyen/WinLic/main/WinLicPS/          ║
; ║          settings.default.ini                       ║
; ║  Last-Updated: <timestamp written by update fn>     ║
; ╚══════════════════════════════════════════════════════╝

[GvlkKeys]
; Full Windows Generic Volume License Keys (source: Microsoft Learn)
; App/script matches WMI PartialProductKey (last 5 chars) against these.
W269N-WFGWX-YVC9B-4J6C9-T83GX = Windows 11/10 Pro
...

[KmsPiracyDomains]
; Known piracy KMS hostnames
kms8.msguides.com
...

[ExtraPortsToScan]
...

[ExtraServiceNames]
...

[ExtraTaskNames]
...

[ExtraProcessNames]
...

[ExtraFilePaths]
...

; ════════════════════════════════════════════════════════
; USER SETTINGS — Edit freely. Never overwritten by updates.
; ════════════════════════════════════════════════════════

[UserGvlkKeys]
; Add custom GVLK keys here (same format as [GvlkKeys])

[UserKmsPiracyDomains]
; Add your own KMS piracy hostnames here

[UserExtraPortsToScan]
; Additional ports beyond defaults

[UserExtraServiceNames]
[UserExtraTaskNames]
[UserExtraProcessNames]
[UserExtraFilePaths]
```

### Two-file strategy for auto-update

- **`settings.default.ini`** — published in the repo; contains ONLY the Default block (no `[User*]` sections). This is the file downloaded during auto-update.
- **`settings.ini`** — local working file = Default block (overwritten by update) + User block (preserved). Shipped with the release as the initial combined file.

### Parser behavior (both App and CLI)

- `AllGvlkKeySuffixes` = last 5 chars of every key in `[GvlkKeys]` ∪ `[UserGvlkKeys]`
- `AllKmsPiracyDomains` = `[KmsPiracyDomains]` ∪ `[UserKmsPiracyDomains]`
- `AllExtraPorts` = `[ExtraPortsToScan]` ∪ `[UserExtraPortsToScan]`
- Same merge pattern for Services, Tasks, Processes, Files

### Auto-update mechanism

**Trigger:** GUI — new "Update defaults" button in the settings area; CLI — new sub-option in Option 7 menu ("U — Update scan defaults from GitHub")

**Process (both App and CLI):**
1. Check internet connectivity (already implemented)
2. Download `https://raw.githubusercontent.com/ardennguyen/WinLic/main/WinLicPS/settings.default.ini`
3. In local `settings.ini`, find the USER SETTINGS marker line:
   `; ════════ USER SETTINGS`
4. Replace everything BEFORE the marker with downloaded content + updated timestamp comment
5. Preserve everything FROM the marker onward (user sections)
6. Save and reload settings

---

## Part B — Complete GVLK Key Table

Source: [Microsoft Learn — KMS client activation keys](https://learn.microsoft.com/en-us/windows-server/get-started/kms-client-activation-keys) (updated 2026-04-02)

### Windows 11 / Windows 10 Semi-Annual Channel

| Edition | Full GVLK | Last 5 |
|---|---|---|
| Win 11/10 Pro | `W269N-WFGWX-YVC9B-4J6C9-T83GX` | T83GX |
| Win 11/10 Pro N | `MH37W-N47XK-V7XM9-C7227-GCQG9` | GCQG9 |
| Win 11/10 Pro for Workstations | `NRG8B-VKK3Q-CXVCJ-9G2XF-6Q84J` | 6Q84J |
| Win 11/10 Pro for Workstations N | `9FNHH-K3HBT-3W4TD-6383H-6XYWF` | 6XYWF |
| Win 11/10 Pro Education | `6TP4R-GNPTD-KYYHQ-7B7DP-J447Y` | J447Y |
| Win 11/10 Pro Education N | `YVWGF-BXNMC-HTQYQ-CPQ99-66QFC` | 66QFC |
| Win 11/10 Education | `NW6C2-QMPVW-D7KKK-3GKT6-VCFB2` | VCFB2 |
| Win 11/10 Education N | `2WH4N-8QGBV-H22JP-CT43Q-MDWWJ` | MDWWJ |
| Win 11/10 Enterprise | `NPPR9-FWDCX-D2C8J-H872K-2YT43` | 2YT43 |
| Win 11/10 Enterprise N | `DPH2V-TTNVB-4X9Q3-TJR4H-KHJW4` | KHJW4 |
| Win 11/10 Enterprise G | `YYVX9-NTFWV-6MDM3-9PT4T-4M68B` | 4M68B |
| Win 11/10 Enterprise G N | `44RPN-FTY23-9VTTB-MP9BX-T84FV` | T84FV |

### Windows 11 LTSC / Windows 10 LTSC/LTSB

| Edition | Full GVLK | Last 5 |
|---|---|---|
| Win 11 Enterprise LTSC 2024 / Win 10 LTSC 2021/2019 | `M7XTQ-FN8P6-TTKYV-9D4CC-J462D` | J462D |
| Win 11/10 Enterprise N LTSC | `92NFX-8DJQP-P6BBQ-THF9C-7CG2H` | 7CG2H |
| Win IoT Enterprise LTSC 2024/2021 | `KBN8V-HFGQ4-MGXVD-347P6-PDQGT` | PDQGT |
| Win 10 Enterprise LTSB 2016 | `DCPHK-NFMTC-H88MJ-PFHPY-QJ4BJ` | QJ4BJ |
| Win 10 Enterprise N LTSB 2016 | `QFFDN-GRT3P-VKWWX-X7T3R-8B639` | 8B639 |
| Win 10 Enterprise LTSB 2015 | `WNMTR-4C88C-JK8YV-HQ7T2-76DF9` | 76DF9 |
| Win 10 Enterprise N LTSB 2015 | `2F77B-TNFGY-69QQF-B8YKP-D69TJ` | D69TJ |

### Windows 8.1 (legacy — TSforge covers Win7+)

| Edition | Full GVLK | Last 5 |
|---|---|---|
| Win 8.1 Pro | `GCRJD-8NW9H-F2CDX-CCM8D-9D6T9` | 9D6T9 |
| Win 8.1 Pro N | `HMCNV-VVBFX-7HMBH-CTY9B-B4FXY` | B4FXY |
| Win 8.1 Enterprise | `MHF9N-XY6XB-WVXMC-BTDCT-MKKG7` | MKKG7 |
| Win 8.1 Enterprise N | `TT4HM-HN7YT-62K67-RGRQJ-JFFXW` | JFFXW |

> **Note:** Windows 10/11 Home editions do NOT have a GVLK — Home is not available via volume licensing. If a Home edition shows a KMS channel, that alone is already suspicious.

---

## Part C — Detection Layers

### Layer 1 — GVLK Key + Activation Channel Check (ERROR)

**Implementation:** WMI `Win32_SoftwareLicensingProduct` query (Win10/11 compatible)

```
SELECT PartialProductKey, LicenseStatus, Description, GracePeriodRemaining, LicenseFamily
FROM Win32_SoftwareLicensingProduct
WHERE PartialProductKey IS NOT NULL AND ApplicationID = '55c92734-d682-4d71-983e-d6ec3f16059f'
```
*(ApplicationID is Windows; separate query for Office)*

**Logic:**
```
PartialProductKey (last 5 chars) ∈ AllGvlkKeySuffixes
  AND LicenseStatus = 1 (Licensed)
  AND GracePeriodRemaining = 0 (permanent — no active KMS countdown)
→ ERROR: "GVLK detected with permanent activation — likely HWID/TSforge piracy"

PartialProductKey ∈ AllGvlkKeySuffixes
  AND GracePeriodRemaining > 0
  AND NO KMS server configured (KeyManagementServiceName is empty)
→ WARN: "GVLK installed but no KMS server — may indicate KMS38 legacy or misconfiguration"

LicenseFamily = "Windows" AND Description contains "VOLUME_KMSCLIENT"
  AND machine is NOT domain-joined
  AND KeyManagementServiceName is empty or external
→ WARN: "Volume license channel on non-enterprise machine"
```

---

### Layer 2 — Activation Expiry Analysis (WMI, ERROR/WARN)

**WMI property:** `GracePeriodRemaining` (in minutes)

**Additional check:** Parse `slmgr /dlv` for activation channel "Phone" (TSforge ZeroCID indicator)
via WMI `Win32_SoftwareLicensingProduct.Description`

```
GracePeriodRemaining = 0 → "Permanent activation" (combine with Layer 1 for GVLK check)

Convert GracePeriodRemaining minutes → expiry date:
  ExpiryDate = Now + GracePeriodRemaining minutes

ExpiryDate.Year ≥ 2037:
  → ERROR: "KMS38 legacy activation detected (expiry: 2038-01-19)"

ExpiryDate.Year ≥ 2100:
  → ERROR: "TSforge KMS4k activation detected (anomalous far-future expiry)"

ExpiryDate within 165–195 days from today (180 ± 15 days):
  AND KeyManagementServiceName is external/public
  → WARN: "Online KMS 180-day activation cycle detected"

Description contains "phone" (case-insensitive):
  → ERROR: "Phone activation channel on a machine with no prior phone activation — possible TSforge ZeroCID"
```

---

### Layer 3 — Online KMS Complete Artifact Set (updates to existing)

**Additions beyond existing KMS checks:**

**Scheduled task additions:**
- `\Activation-Renewal` — exact MAS Online KMS task name (ERROR)

**File additions:**
- `C:\Program Files\Activation-Renewal\Activation_task.cmd` (ERROR)
- `C:\Program Files\Activation-Renewal\Info.txt` (WARN)

**Registry additions:**
- `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform\KeyManagementServiceName` = `10.0.0.10` → ERROR (bogus placeholder)
- `HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform\KeyManagementServiceName` → same check (currently missing)
- `HKLM\SOFTWARE\Microsoft\OfficeSoftwareProtectionPlatform\KeyManagementServiceName` → run 5-way classifier on this too (Office KMS, currently not checked)
- `HKLM\SOFTWARE\Microsoft\OfficeSoftwareProtectionPlatform\KeyManagementServicePort` = `1688` (flag if paired with external host)

---

### Layer 4 — KMS38 File Additions (extends existing file scan)

Add to `[ExtraFilePaths]` in `settings.default.ini`:
```
C:\ProgramData\Microsoft\Windows\ClipSVC\GenuineTicket\GenuineTicket.xml
```

Add to `[ExtraProcessNames]` scan (transient but worth checking):
```
gatherosstate.exe
clipup.exe
```

Explicit message: if `gatherosstate.exe` is running from outside `%SystemRoot%\System32\` → ERROR

---

### Layer 5 — TSforge Data.dat Timestamp Check (WARN, LOW CONFIDENCE)

```
File: C:\Windows\System32\spp\store\2.0\data.dat

Check:
  LastWriteTime vs Windows Install Date
  (HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\InstallDate)

If LastWriteTime > InstallDate AND
   LastWriteTime is NOT within 48h of any Windows Update event (Event ID 19 in System log):
  → WARN: "[LOW CONFIDENCE] SPP store file modified outside Windows Update context
           — may indicate TSforge activation (also possible after legitimate troubleshooting)"
```

---

### Layer 6 — SPP Event Log Check (WARN)

```
Query: System event log, Source = Microsoft-Windows-Security-SPP
Events: ID 12288 (activation attempt), 12289 (response), 12290 (KMS request), 8198 (success/fail)

If Event 12290 (KMS request) present AND server address in log is external (non-RFC-1918):
  → ERROR: "SPP event log shows KMS activation request to external server"

If Events 12288/12289 present with no preceding Windows Update or legitimate activation context:
  → WARN: "SPP activation events in log — verify activation is legitimate"
```

---

### Layer 7 — Legal Notice (always shown at end of Option 7)

```
─────────────────────────────────────────────
 ⚠ ACTIVATION AUDIT — LEGAL NOTICE
─────────────────────────────────────────────
 Using Windows without a genuine license
 purchased from Microsoft or an authorized
 reseller violates Microsoft's EULA (§4).
 Enterprise/OEM: verify with IT or your OEM.
 Check your license: https://aka.ms/MyAccount

 KNOWN SCAN LIMITATIONS:
 • HWID via MAS obtains a real Microsoft
   digital license — undetectable by design.
 • Tools removed after use leave no trace.
 • Corporate KMS may trigger some indicators.
─────────────────────────────────────────────
```

---

## Part D — Files to Modify

| File | Changes |
|---|---|
| `WinLicPS/settings.ini` | Full restructure: Default block + User block; add `[GvlkKeys]`, `[UserGvlkKeys]`, `[UserKmsPiracyDomains]`, etc.; add all GVLK keys |
| `WinLicPS/settings.default.ini` | **NEW** — default-only version of settings.ini for repo distribution and auto-update download |
| `WinLicPS/WinLicManager.ps1` | New detection layers 1–7; `Update-DefaultSettings` function; new INI section merge; `Read-WmiLicensing` helper |
| `WinLicApp/AppSettings.cs` | Parse `[User*]` sections and merge; `GvlkKeySuffixes` property; `UpdateDefaultsAsync()` method |
| `WinLicApp/MainWindow.xaml.cs` | New Option 7 checks for all layers; `CheckActivationChannel()`, `CheckExpiryAnomaly()`, `CheckSppEventLog()`, `CheckTSforgeTimestamp()` |
| `WinLicApp/Localization.cs` | New strings for all new check results (EN + VI) |

---

## Part E — Verification Plan

```powershell
dotnet build WinLicApp\WinLicApp.csproj -c Release
```

**Manual tests:**
1. Clean VM → Option 7 → all green, legal notice shown
2. Machine with `KeyManagementServiceName = 10.0.0.10` → ERROR for bogus placeholder
3. Machine with `KeyManagementServiceName = kms8.msguides.com` → ERROR (piracy domain)
4. Machine with GracePeriodRemaining ≈ 180-day window → WARN for Online KMS cycle
5. GitHub auto-update: click "Update defaults" → `settings.ini` default block replaced, user block preserved

---

## Build Order

1. `settings.default.ini` (new file — data only, no code)
2. `settings.ini` (restructured)
3. `AppSettings.cs` (parser + merge + GVLK + auto-update)
4. `Localization.cs` (new strings)
5. `MainWindow.xaml.cs` (new detection logic)
6. `WinLicManager.ps1` (new detection logic + update function)
7. Build + smoke test
