$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$installer = Get-Content (Join-Path $root 'installer\LicenseScope.iss') -Raw
$metadata = Get-Content (Join-Path $root 'installer\includes\ProductMetadata.iss') -Raw
$settings = Get-Content (Join-Path $root 'LicenseScope.App\ApplicationDataPaths.cs') -Raw
$smoke = Get-Content (Join-Path $root 'build\Invoke-InstallerSmoke.ps1') -Raw

$oldProductionAppId = '9DF7FE16-6903-4B48-98FB-866D77B31C7A'
$oldTestAppId = '8E9E3612-C2B0-4A58-ACB4-8D1BEA0FE519'
$newProductionAppId = '0C329754-97C0-45BE-8664-347B26EDA0E0'
$newTestAppId = '98FB3801-EC81-49A7-987A-7742D6D01687'

$checks = [ordered]@{
    'Independent production AppId' = $metadata -match $newProductionAppId -and $metadata -notmatch $oldProductionAppId
    'Independent test AppId' = $installer -match $newTestAppId -and $installer -notmatch $oldTestAppId
    'Separate install directory' = $installer -match "autopf64}\\LicenseScope" -and $installer -notmatch "autopf64}\\WinLic"
    'Separate settings directory' = $settings -match 'LocalApplicationData' -and $settings -match '"LicenseScope"' -and $settings -notmatch '"WinLic"'
    'Separate executables' = $metadata -match 'LicenseScope\.App\.exe' -and $metadata -match 'LicenseScope\.Cli\.exe'
    'No WinLic registry mutation' = $installer -notmatch 'WinLic' -and $installer -notmatch 'RegDeleteKey[^\r\n]*WinLic'
    'No WinLic filesystem cleanup' = $installer -notmatch 'UninstallDelete[^\r\n]*WinLic' -and $installer -notmatch 'DelTree[^\r\n]*WinLic'
    'Runtime canary protected' = $smoke -match 'protected-winlic' -and $smoke -match 'Protected WinLic coexistence canary'
}
$failed = @($checks.GetEnumerator() | Where-Object { -not $_.Value })
if ($failed) { throw ('Installer coexistence failures: ' + (($failed.Name) -join ', ')) }
Write-Host "InstallerCoexistence: $($checks.Count) passed"
