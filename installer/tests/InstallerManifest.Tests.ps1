$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$path = Join-Path $root 'installer\prerequisites\dotnet48-runtime.json'
$m = Get-Content $path -Raw | ConvertFrom-Json
$checks = [ordered]@{
  'Schema' = $m.schemaVersion -eq 1
  'Exact runtime name' = $m.fileName -eq 'NDP48-x86-x64-AllOS-ENU.exe'
  'Exact size' = $m.sizeBytes -eq 121346568
  'SHA-256 format' = $m.sha256 -match '^[A-F0-9]{64}$'
  'Offline runtime type' = $m.packageType -eq 'offline-runtime-redistributable'
  'Not web/developer/targeting' = $m.packageType -notmatch 'web|developer|targeting'
  'Microsoft signer' = $m.signerSubject -match '^CN=Microsoft Corporation'
  'Release key' = $m.minimumReleaseKey -eq 528040
  'ARM64 no prerequisite execution' = $m.arm64Policy -match '^do-not-run'
}
$failed = @($checks.GetEnumerator() | Where-Object { -not $_.Value })
if ($failed) { throw ('Prerequisite manifest failures: ' + (($failed.Name) -join ', ')) }
Write-Host "InstallerManifest: $($checks.Count) passed"
