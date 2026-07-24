$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$source = Get-Content (Join-Path $root 'installer\LicenseScope.iss') -Raw
$checks = [ordered]@{
  'Windows 8.0 hard block' = $source -match 'V\.Minor = 2'
  'Windows 7 SP1 gate' = $source -match 'ServicePackMajor < 1'
  'ARM64 uses x86' = $source -match 'IsX64OS and \(not NativeArm64\)'
  'ARM64 prerequisite block' = $source -match 'NativeArm64 and \(DotNetRelease < DotNet48Release\)'
  'Release key 528040' = $source -match 'DotNet48Release = 528040'
  'Offline silent switches' = $source -match '/q /norestart /ChainingPackage LicenseScope'
  'No framework download' = $source -notmatch 'Download|https?://'
  'No activation commands' = $source -notmatch '/ato|/ipk|/upk|/rearm|/cpky'
  'Downgrade blocked' = $source -match 'CompareVersion\(Existing'
  'Per-machine elevation' = $source -match 'PrivilegesRequired=admin'
  'Application starts de-elevated' = $source -match 'runasoriginaluser'
  'No automatic scanner' = $source -notmatch 'audit --all'
  'License Scope product name' = $source -match 'ProductName'
  'License Scope install directory' = $source.Contains('autopf64}\LicenseScope') -and $source.Contains('autopf32}\LicenseScope')
  'License Scope GUI executable' = $source -match 'ProductExe'
  'License Scope CLI executable' = $source -match 'CliExe'
  'Architecture build forces rebuild' = (Get-Content (Join-Path $root 'build\Build-Installer.ps1') -Raw) -match '-t:Rebuild'
  'Architecture check compares machine field' = (Get-Content (Join-Path $root 'build\Build-Installer.ps1') -Raw) -match '\.Machine -eq \$expected'
}
$failed = @($checks.GetEnumerator() | Where-Object { -not $_.Value })
if ($failed) { throw ('Installer policy failures: ' + (($failed.Name) -join ', ')) }
Write-Host "InstallerPolicy: $($checks.Count) passed"
