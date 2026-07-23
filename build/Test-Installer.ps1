[CmdletBinding()]
param([string]$SetupPath = "$PSScriptRoot\..\artifacts\installer\LicenseScope-Setup.exe")
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot\..\installer\tests\InstallerPolicy.Tests.ps1"
& "$PSScriptRoot\..\installer\tests\InstallerManifest.Tests.ps1"
if (-not (Test-Path $SetupPath)) { throw "Setup not found: $SetupPath" }
$signature = Get-AuthenticodeSignature $SetupPath
if ($signature.Status -ne 'NotSigned') { throw "Unexpected Setup signing state: $($signature.Status)" }
$strings = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($SetupPath))
$unicodeStrings = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes($SetupPath))
foreach ($forbidden in '.pdb','LicenseScope.Core.Tests','/ato','/ipk','/upk','/cpky','/rearm','/skms','/ckms') {
  if ($strings.IndexOf($forbidden, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
      $unicodeStrings.IndexOf($forbidden, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw "Forbidden Setup content: $forbidden"
  }
}
Write-Host 'Installer static checks: passed'
