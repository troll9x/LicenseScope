# WinLic Expanded Detection — Task List

## Phase 1: Settings files
- [x] Create `settings.default.ini` (default block only, for auto-update download)
- [x] Restructure `settings.ini` (Default + User sections with full GVLK table)

## Phase 2: AppSettings.cs
- [x] Add `[GvlkKeys]` / `[UserGvlkKeys]` parser (key = description format)
- [x] Add `AllGvlkSuffixes` merged property (last 5 chars)
- [x] Add `[UserKmsPiracyDomains]` section (separate from default `[KmsPiracyDomains]`)
- [x] Add `UpdateDefaultsAsync()` method (HttpClient download → replace default block)
- [x] Fix StreamWriter constructor for .NET Framework 4.8

## Phase 3: Localization.cs
- [x] Add EN + VI strings for new detection checks ①-⑨
- [x] GVLK / channel / expiry / SPP / TSforge / legal notice / update-defaults strings

## Phase 4: MainWindow.xaml.cs
- [x] KMS host: add WoW64 registry path check
- [x] KMS host: add bogus 10.0.0.10 placeholder IP detection
- [x] Layer 5 file paths: add GenuineTicket.xml + Activation-Renewal artifacts
- [x] Layer 7: GVLK + activation channel check (WMI Win32_SoftwareLicensingProduct)
  - Phone activation anomaly (TSforge ZeroCID)
  - GVLK + permanent = piracy flag
  - Office KMS registry bonus check
- [x] Layer 8: Activation expiry analysis (KMS38 / TSforge / 180-day)
- [x] Layer 9: TSforge SPP data.dat timestamp (LOW CONFIDENCE)
- [x] Legal notice at end of scan
- [x] Summary: show total indicator count
- [ ] Add "Update defaults from GitHub" button in Audit Settings panel (UI)

## Phase 5: WinLicManager.ps1
- [x] Update `Get-ScanLists` to load GvlkKeys + UserKmsPiracyDomains merge
- [x] Add Activation-Renewal to DEFAULT_SERVICES + DEFAULT_TASKS
- [x] Add gatherosstate to DEFAULT_PROCS
- [x] Add bogus 10.0.0.10 KMS detection
- [x] Add Office KMS registry check (1f)
- [x] Add GenuineTicket.xml + Activation-Renewal to built-in file paths
- [x] Layer 7: GVLK + activation channel check
- [x] Layer 8: Expiry analysis (KMS38/TSforge/180-day)
- [x] Layer 9: SPP data.dat timestamp (LOW CONFIDENCE)
- [x] Legal notice at end
- [x] Updated preamble to describe all 9 detection checks
- [x] Summary: show total indicators flagged

## Phase 6: Verification
- [x] dotnet build — SUCCEEDED (0 errors, 9 pre-existing warnings)
- [ ] Smoke test Option 7 on current machine
