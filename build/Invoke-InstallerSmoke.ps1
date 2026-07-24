[CmdletBinding()]
param(
    [ValidateSet('x64','x86')][string]$ExpectedPayload = 'x64',
    [string]$SetupPath = '',
    [switch]$Production,
    [ValidateSet('Install','Uninstall')][string]$ProductionAction = 'Install',
    [string]$OutputDirectory = ''
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent

if ($Production) {
    $principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Production action requires an elevated token.' }
    if (-not $OutputDirectory) { throw 'OutputDirectory is required for production smoke.' }
    New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
    $resultPath = Join-Path $OutputDirectory ("$($ProductionAction.ToLowerInvariant())-result.json")
    $installPath = Join-Path $env:ProgramFiles 'LicenseScope'
    $uninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{0C329754-97C0-45BE-8664-347B26EDA0E0}_is1'
    $uninstallKey32 = 'Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{0C329754-97C0-45BE-8664-347B26EDA0E0}_is1'
    if ($ProductionAction -eq 'Install') {
        if (-not (Test-Path -LiteralPath $SetupPath -PathType Leaf)) { throw 'Production SetupPath was not found.' }
        $logPath = Join-Path $OutputDirectory 'production-install.log'
        $process = Start-Process -FilePath $SetupPath -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',"/LOG=$logPath") -Wait -PassThru
        $entries = @($uninstallKey,$uninstallKey32 | ForEach-Object { Get-ItemProperty -Path "Registry::HKEY_LOCAL_MACHINE\$_" -ErrorAction SilentlyContinue })
        $entry = $entries | Select-Object -First 1
        $manifestPath = Join-Path $installPath 'installation-manifest.json'
        $manifest = if (Test-Path $manifestPath) { Get-Content $manifestPath -Raw | ConvertFrom-Json } else { $null }
        [ordered]@{
            action='Install'; exitCode=$process.ExitCode; elevated=$true; installPath=$installPath
            uninstallEntryFound=($null -ne $entry); uninstallEntryCount=$entries.Count; displayVersion=$entry.DisplayVersion
            payload=$manifest.payload; frameworkInstalledBySetup=$manifest.frameworkInstalledBySetup
            guiExists=(Test-Path (Join-Path $installPath 'LicenseScope.App.exe'))
            cliExists=(Test-Path (Join-Path $installPath 'LicenseScope.Cli.exe'))
        } | ConvertTo-Json | Set-Content $resultPath -Encoding UTF8
        if ($process.ExitCode -notin 0,1641,3010 -or $entries.Count -ne 1 -or $manifest.payload -ne 'x64' -or $manifest.frameworkInstalledBySetup) { throw 'Production installation verification failed.' }
    } else {
        $uninstaller = Join-Path $installPath 'unins000.exe'
        if (-not (Test-Path $uninstaller)) { throw 'Production uninstaller was not found.' }
        $logPath = Join-Path $OutputDirectory 'production-uninstall.log'
        $process = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',"/LOG=$logPath") -Wait -PassThru
        Start-Sleep -Seconds 2
        $entries = @($uninstallKey,$uninstallKey32 | ForEach-Object { Get-ItemProperty -Path "Registry::HKEY_LOCAL_MACHINE\$_" -ErrorAction SilentlyContinue })
        $remaining = @(Get-ChildItem -LiteralPath $installPath -Force -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
        [ordered]@{action='Uninstall';exitCode=$process.ExitCode;elevated=$true;uninstallEntryCount=$entries.Count;remainingFiles=$remaining} |
            ConvertTo-Json | Set-Content $resultPath -Encoding UTF8
        if ($process.ExitCode -ne 0 -or $entries.Count -ne 0 -or $remaining.Count -ne 0) { throw 'Production uninstall verification failed.' }
    }
    Write-Host "Production $ProductionAction completed. Evidence: $resultPath"
    exit 0
}

$name = "LicenseScope-Smoke-$ExpectedPayload"
$buildArgs = @{ SkipTests = $true; TestInstallMode = $true; OutputBaseFilename = $name }
if ($ExpectedPayload -eq 'x86') { $buildArgs.TestForceX86 = $true }
& (Join-Path $PSScriptRoot 'Build-Installer.ps1') @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Smoke Setup build failed.' }
$setup = Join-Path $repo "artifacts\installer\$name.exe"
$install = Join-Path $repo "artifacts\smoke\$ExpectedPayload\app"
$reports = Join-Path $repo "artifacts\smoke\$ExpectedPayload\reports"
$protectedWinLic = Join-Path $repo "artifacts\smoke\$ExpectedPayload\protected-winlic"
$protectedWinLicCanary = Join-Path $protectedWinLic 'coexistence.canary'
$log = Join-Path $repo "artifacts\smoke\$ExpectedPayload\install.log"
New-Item -ItemType Directory -Force (Split-Path $install -Parent),$reports,$protectedWinLic | Out-Null
Set-Content -LiteralPath $protectedWinLicCanary -Value 'WinLic coexistence fixture: do not touch' -Encoding ASCII
$p = Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',"/DIR=$install", "/LOG=$log") -Wait -PassThru
if ($p.ExitCode -notin 0,1641,3010) { throw "Smoke install failed: $($p.ExitCode)" }
$manifest = Get-Content (Join-Path $install 'installation-manifest.json') -Raw | ConvertFrom-Json
if ($manifest.payload -ne $ExpectedPayload) { throw "Expected $ExpectedPayload, installed $($manifest.payload)." }
if ($manifest.frameworkInstalledBySetup) { throw 'Smoke unexpectedly invoked the framework prerequisite.' }
$cli = Join-Path $install 'LicenseScope.Cli.exe'
$sample = Join-Path $install 'Samples\license-audit-simulation.json'
if (-not (Test-Path -LiteralPath $sample -PathType Leaf)) { throw 'Installed simulation fixture is missing.' }
$version = & $cli --version
if ($LASTEXITCODE -ne 0 -or -not $version) { throw 'Installed CLI startup failed.' }
$auditOutput = & $cli audit --all --format json,csv,html --output $reports --overwrite 2>&1
$auditExit = $LASTEXITCODE
if ($auditExit -notin 0,1,2) { throw "Installed CLI audit failed: $auditExit`n$auditOutput" }
foreach ($extension in 'json','csv','html') {
    if (-not (Get-ChildItem $reports -Filter "*.$extension" -File)) { throw "Missing $extension report." }
}
$gui = Start-Process -FilePath (Join-Path $install 'LicenseScope.App.exe') -PassThru
Start-Sleep -Seconds 3
if ($gui.HasExited) { throw "Installed GUI exited during startup ($($gui.ExitCode))." }
$gui.CloseMainWindow() | Out-Null
if (-not $gui.WaitForExit(5000)) { $gui.Kill(); $gui.WaitForExit() }
$settingsDir = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'LicenseScope'
New-Item -ItemType Directory -Force $settingsDir | Out-Null
$sentinel = Join-Path $settingsDir 'phase9-preservation.sentinel'
Set-Content $sentinel 'preserve-on-uninstall' -Encoding ASCII
$uninstaller = Join-Path $install 'unins000.exe'
$u = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -Wait -PassThru
if ($u.ExitCode -ne 0) { throw "Uninstall failed: $($u.ExitCode)" }
if ((Test-Path $install) -and (Get-ChildItem $install -Force)) {
    Start-Sleep -Seconds 3
    try { Remove-Item -LiteralPath $install -Recurse -Force -ErrorAction Stop } catch { throw "Installed files remain locked after uninstall: $($_.Exception.Message)" }
}
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{98FB3801-EC81-49A7-987A-7742D6D01687}_is1'
if (Test-Path $uninstallKey) { throw 'Uninstall registry entry remains.' }
if (Test-Path $install) { Remove-Item -LiteralPath $install -Force }
if (-not (Test-Path $sentinel)) { throw 'User settings were removed by uninstall.' }
if (-not (Test-Path -LiteralPath $protectedWinLicCanary)) { throw 'Protected WinLic coexistence canary was modified or removed.' }
Remove-Item -LiteralPath $sentinel -Force
Write-Host "Smoke $ExpectedPayload passed; CLI audit exit=$auditExit; reports=json,csv,html; License Scope settings and WinLic canary preserved."
