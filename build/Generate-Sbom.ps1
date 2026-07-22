[CmdletBinding()]
param([Parameter(Mandatory)][string]$SbomToolPath,[Parameter(Mandatory)][string]$DropPath,[Parameter(Mandatory)][string]$ComponentPath,[Parameter(Mandatory)][string]$OutputDirectory,[string]$Version='1.0.0',[string]$ExpectedVersion='4.1.5',[string]$ExpectedSha256='')
$ErrorActionPreference='Stop'
if(-not(Test-Path $SbomToolPath -PathType Leaf)){throw 'SBOM tool missing.'}
$v=(& $SbomToolPath --version 2>&1)-join''
if($LASTEXITCODE-ne 0-or$v.Trim()-ne$ExpectedVersion){throw "SBOM tool version mismatch: $v"}
$pe=& "$PSScriptRoot\Get-PEArchitecture.ps1" $SbomToolPath
if($pe.Machine-ne'X64'){throw 'SBOM tool must be x64.'}
$sig=Get-AuthenticodeSignature $SbomToolPath
if($sig.Status-ne'Valid'-or$sig.SignerCertificate.Subject-notmatch '^CN=Microsoft Corporation'){throw 'SBOM tool trust verification failed.'}
$hash=(Get-FileHash $SbomToolPath -Algorithm SHA256).Hash
if($ExpectedSha256-and$hash-ne$ExpectedSha256){throw 'SBOM tool hash mismatch.'}
New-Item -ItemType Directory -Force $OutputDirectory|Out-Null
$telemetry=Join-Path $env:TEMP ('winlic-sbom-'+[guid]::NewGuid()+'.json')
try {
  $generationOutput=@(& $SbomToolPath generate -b $DropPath -bc $ComponentPath -pn WinLic -pv $Version -ps WinLic -nsb https://github.com/ardennguyen/WinLic -nsu ("WinLic-"+$Version) -mi SPDX:2.2 -li false -F false -t $telemetry -m $OutputDirectory -D true -V Warning 2>&1)
  if($LASTEXITCODE){throw 'SBOM generation failed.'}
  $manifest=Get-ChildItem $OutputDirectory -Recurse -Filter manifest.spdx.json|Select-Object -First 1
  if(-not$manifest){throw 'SPDX manifest missing.'}
  $validation=Join-Path $OutputDirectory 'validation.json'
  $validationOutput=@(& $SbomToolPath validate -b $DropPath -m (Join-Path $OutputDirectory '_manifest') -o $validation -mi SPDX:2.2 -F false -V Warning 2>&1)
  if($LASTEXITCODE){throw 'SBOM validation failed.'}
  $final=Join-Path $OutputDirectory ("WinLic-$Version-sbom.spdx.json")
  Copy-Item $manifest.FullName $final -Force
  $raw=Get-Content $final -Raw
  if($raw-match 'C:\\Users\\|D:\\Tools\\|product.?key|access.?token|refresh.?token|BEGIN PRIVATE KEY'){throw 'SBOM privacy scan failed.'}
  $json=$raw|ConvertFrom-Json
  if($json.spdxVersion-ne'SPDX-2.2'){throw 'Unexpected SPDX version.'}
  Write-Output $final
} finally {if(Test-Path $telemetry){Remove-Item $telemetry -Force}}
