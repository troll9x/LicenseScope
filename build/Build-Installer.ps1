[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$InnoCompiler = 'C:\Program Files\Inno Setup 7\ISCC.exe',
    [switch]$SkipTests,
    [switch]$TestForceX86,
    [switch]$TestInstallMode,
    [string]$OutputBaseFilename = 'LicenseScope-Setup',
    [string]$ArtifactSubdirectory = 'installer',
    [string]$VersionOverride = ''
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$installer = Join-Path $repo 'installer'
$artifactRoot = Join-Path $repo ('artifacts\' + $ArtifactSubdirectory)
$stageRoot = Join-Path $repo 'artifacts\staging'
$manifestPath = Join-Path $installer 'prerequisites\dotnet48-runtime.json'
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$prerequisite = Join-Path $installer ('prerequisites\cache\' + $manifest.fileName)

function Assert-Condition([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Reset-Directory([string]$Path) {
    if (Test-Path -LiteralPath $Path) {
        $resolved = [IO.Path]::GetFullPath($Path)
        Assert-Condition ($resolved.StartsWith([IO.Path]::GetFullPath((Join-Path $repo 'artifacts')), [StringComparison]::OrdinalIgnoreCase)) "Refusing to clean outside artifacts: $resolved"
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Path | Out-Null
}
function Verify-Prerequisite {
    Assert-Condition (Test-Path -LiteralPath $prerequisite) "Offline prerequisite not found: $prerequisite"
    $file = Get-Item -LiteralPath $prerequisite
    Assert-Condition ($file.Length -eq [long]$manifest.sizeBytes) 'Offline prerequisite size mismatch.'
    Assert-Condition ((Get-FileHash -LiteralPath $prerequisite -Algorithm SHA256).Hash -eq $manifest.sha256) 'Offline prerequisite SHA-256 mismatch.'
    $version = $file.VersionInfo
    Assert-Condition ($version.ProductName -eq $manifest.productName) 'Offline prerequisite product mismatch.'
    Assert-Condition ($version.ProductVersion -eq $manifest.productVersion) 'Offline prerequisite product version mismatch.'
    Assert-Condition ($version.FileVersion -eq $manifest.fileVersion) 'Offline prerequisite file version mismatch.'
    $signature = Get-AuthenticodeSignature -LiteralPath $prerequisite
    Assert-Condition ($signature.Status -eq 'Valid') "Offline prerequisite signature is $($signature.Status)."
    Assert-Condition ($signature.SignerCertificate.Subject -eq $manifest.signerSubject) 'Offline prerequisite signer mismatch.'
}
function Copy-Payload([string]$Architecture, [string]$Destination) {
    Reset-Directory $Destination
    dotnet build (Join-Path $repo 'LicenseScope.sln') -c $Configuration -t:Rebuild --no-restore --verbosity minimal /p:PlatformTarget=$Architecture /p:Prefer32Bit=false
    if ($LASTEXITCODE -ne 0) { throw "$Architecture build failed." }
    $sources = @(
        (Join-Path $repo "LicenseScope.App\bin\$Configuration\net48"),
        (Join-Path $repo "src\LicenseScope.Cli\bin\$Configuration\net48")
    )
    foreach ($source in $sources) {
        Get-ChildItem -LiteralPath $source -File | Where-Object { $_.Extension -in '.exe','.dll','.config' } | Copy-Item -Destination $Destination -Force
    }
    $sampleSource = Join-Path $repo "LicenseScope.App\bin\$Configuration\net48\Samples"
    $sampleDestination = Join-Path $Destination 'Samples'
    Assert-Condition (Test-Path -LiteralPath $sampleSource -PathType Container) "Samples directory missing from $Architecture application output."
    Copy-Item -LiteralPath $sampleSource -Destination $sampleDestination -Recurse -Force
    foreach ($required in 'LicenseScope.App.exe','LicenseScope.Cli.exe') { Assert-Condition (Test-Path (Join-Path $Destination $required)) "$required missing from $Architecture payload." }
    Assert-Condition (Test-Path (Join-Path $sampleDestination 'license-audit-simulation.json')) "Simulation fixture missing from $Architecture payload."
    $forbidden = Get-ChildItem $Destination -Recurse -File | Where-Object { $_.Extension -in '.pdb','.cs','.ps1','.trx' -or $_.Name -match 'Tests' }
    Assert-Condition ($null -eq $forbidden) "$Architecture payload contains forbidden files."
    $peTool = Join-Path $repo 'build\Get-PEArchitecture.ps1'
    $guiMachine = & $peTool (Join-Path $Destination 'LicenseScope.App.exe')
    $cliMachine = & $peTool (Join-Path $Destination 'LicenseScope.Cli.exe')
    $expected = if ($Architecture -eq 'x86') { 'X86' } else { 'X64' }
    Assert-Condition ($guiMachine.Machine -eq $expected) "GUI PE architecture is $($guiMachine.Machine), expected $expected."
    Assert-Condition ($cliMachine.Machine -eq $expected) "CLI PE architecture is $($cliMachine.Machine), expected $expected."
}

Assert-Condition (Test-Path -LiteralPath $InnoCompiler) "Inno Setup compiler not found: $InnoCompiler"
Verify-Prerequisite
dotnet restore (Join-Path $repo 'LicenseScope.sln') --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
if (-not $SkipTests) {
    dotnet test (Join-Path $repo 'LicenseScope.sln') -c $Configuration --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
Reset-Directory $artifactRoot
Reset-Directory $stageRoot
$x86 = Join-Path $stageRoot 'x86'; $x64 = Join-Path $stageRoot 'x64'
Copy-Payload 'x86' $x86
Copy-Payload 'x64' $x64

$version = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $x64 'LicenseScope.App.exe')).FileVersion
if ($version -notmatch '^\d+\.\d+\.\d+\.\d+$') { $version = '1.0.0.0' }
if ($VersionOverride) {
    if ($VersionOverride -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'VersionOverride must contain four numeric parts.' }
    $version = $VersionOverride
}
$defines = @(
    "/DAppVersion=$version", "/DX86Source=$x86", "/DX64Source=$x64",
    "/DPrerequisitePath=$prerequisite", "/DOutputDir=$artifactRoot",
    "/DOutputBaseFilename=$OutputBaseFilename"
)
if ($TestForceX86) { $defines += '/DTestForceX86=1' }
if ($TestInstallMode) { $defines += '/DTestInstallMode=1' }
& $InnoCompiler @defines (Join-Path $installer 'LicenseScope.iss')
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed ($LASTEXITCODE)." }
$setup = Join-Path $artifactRoot ($OutputBaseFilename + '.exe')
Assert-Condition (Test-Path $setup) 'Expected universal Setup output was not created.'
$setupInfo = Get-Item $setup
$buildManifest = [ordered]@{
    schemaVersion = 1; productVersion = $version; generatedUtc = [DateTime]::UtcNow.ToString('o')
    setupFile = $setupInfo.Name; setupSizeBytes = $setupInfo.Length
    setupSha256 = (Get-FileHash $setup -Algorithm SHA256).Hash
    setupAuthenticode = (Get-AuthenticodeSignature $setup).Status.ToString()
    codeSigning = 'UNSIGNED — CODE SIGNING CERTIFICATE NOT PROVIDED'
    payloads = @('x86','x64'); arm64Payload = 'x86-emulated'; prerequisiteSha256 = $manifest.sha256
}
$buildManifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $artifactRoot 'build-manifest.json') -Encoding UTF8
Write-Host "SETUP=$setup"
Write-Host "SHA256=$($buildManifest.setupSha256)"
