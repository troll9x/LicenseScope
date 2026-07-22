$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$dir = Join-Path $repo 'artifacts\smoke\upgrade\app'
& "$PSScriptRoot\Build-Installer.ps1" -SkipTests -TestInstallMode -VersionOverride 0.9.0.0 -ArtifactSubdirectory upgrade-old -OutputBaseFilename WinLic-Old
& "$PSScriptRoot\Build-Installer.ps1" -SkipTests -TestInstallMode -VersionOverride 1.0.0.0 -ArtifactSubdirectory upgrade-new -OutputBaseFilename WinLic-New
$old = Join-Path $repo 'artifacts\upgrade-old\WinLic-Old.exe'
$new = Join-Path $repo 'artifacts\upgrade-new\WinLic-New.exe'
New-Item -ItemType Directory -Force (Split-Path $dir -Parent) | Out-Null
function Install([string]$Setup) {
  $p=Start-Process $Setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',"/DIR=$dir") -Wait -PassThru
  return $p.ExitCode
}
if ((Install $old) -ne 0) { throw 'Old-version install failed.' }
if ((Get-Content "$dir\installation-manifest.json" -Raw | ConvertFrom-Json).productVersion -ne '0.9.0.0') { throw 'Old manifest mismatch.' }
if ((Install $new) -ne 0) { throw 'Upgrade failed.' }
if ((Get-Content "$dir\installation-manifest.json" -Raw | ConvertFrom-Json).productVersion -ne '1.0.0.0') { throw 'Upgrade manifest mismatch.' }
$before=(Get-FileHash "$dir\installation-manifest.json").Hash
$downgrade=Install $old
$after=(Get-FileHash "$dir\installation-manifest.json").Hash
if ($downgrade -eq 0 -or $before -ne $after) { throw 'Downgrade was not safely blocked.' }
$uninstaller=Join-Path $dir 'unins000.exe'
$u=Start-Process $uninstaller -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART' -Wait -PassThru
if ($u.ExitCode -ne 0) { throw 'Upgrade smoke uninstall failed.' }
Write-Host "Upgrade 0.9.0.0 -> 1.0.0.0 passed; downgrade exit=$downgrade and was blocked; uninstall passed."
