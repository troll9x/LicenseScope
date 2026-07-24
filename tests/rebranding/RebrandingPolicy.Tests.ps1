$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$solutionPath = Join-Path $repo 'LicenseScope.sln'
if (-not (Test-Path -LiteralPath $solutionPath)) { throw 'LicenseScope.sln is missing.' }
if (Test-Path -LiteralPath (Join-Path $repo 'WinLic.sln')) { throw 'WinLic.sln must not exist.' }

$solution = Get-Content -LiteralPath $solutionPath -Raw
$projectMatches = [regex]::Matches($solution, '(?m)^Project\("\{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC\}"\) = "([^"]+)", "([^"]+)", "(\{[A-F0-9-]+\})"')
$names = @($projectMatches | ForEach-Object { $_.Groups[1].Value })
$paths = @($projectMatches | ForEach-Object { $_.Groups[2].Value })
$guids = @($projectMatches | ForEach-Object { $_.Groups[3].Value })
$missing = @($paths | Where-Object { -not (Test-Path -LiteralPath (Join-Path $repo $_)) })
$expectedGuids = @(
    '{0732E77F-B7B7-4EC3-858B-05CA6A575A44}','{BD1145BB-E448-42EA-8DB9-E397901CF45A}',
    '{0748802E-D078-4742-BD4E-49A40D6D2272}','{11111111-2222-4333-8444-555555555555}',
    '{66666666-7777-4888-8999-AAAAAAAAAAAA}','{BE8E4482-AF5D-442A-91D1-1287FC7355E8}',
    '{B1ECA0FA-CB2E-4F9C-A0B9-3C96564EC911}','{A4444444-1111-4222-8333-444444444444}',
    '{B4444444-1111-4222-8333-444444444444}','{C4444444-1111-4222-8333-444444444444}',
    '{D4444444-1111-4222-8333-444444444444}','{E4444444-1111-4222-8333-444444444444}',
    '{EE9E8577-9C31-4D4A-A603-3C5BFA927998}','{7799A0E9-B5BB-4AA5-B9AF-101228C57867}',
    '{992450DA-D29D-41F0-BCAC-D844FCCADFA9}','{4735F858-6DD1-42CE-9FAC-13E1832A66ED}',
    '{336D107C-F2CC-4018-A3BB-90033737B567}','{F67D0B84-E56C-4C36-A968-51CEE0E4986B}',
    '{07A21FBF-216F-49BF-A15F-9B3135C0F791}','{3FB8404A-EF91-4FE2-8BB1-7C1FEBB30A19}'
)

$productionFiles = @(
    Get-Item $solutionPath
    Get-ChildItem (Join-Path $repo 'LicenseScope.App') -Recurse -File
    Get-ChildItem (Join-Path $repo 'src') -Recurse -File
    Get-ChildItem (Join-Path $repo '.github') -Recurse -File
) | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$oldBrandHits = foreach ($file in $productionFiles) {
    if ((Get-Content -LiteralPath $file.FullName -Raw -ErrorAction SilentlyContinue) -match 'WinLic(?:Audit|App|\.)') { $file.FullName }
}
$scannerSource = (Get-ChildItem (Join-Path $repo 'src') -Recurse -File -Filter *.cs | Get-Content -Raw) -join "`n"
$sourceFiles = @(
    Get-ChildItem (Join-Path $repo 'src') -Recurse -File -Filter *.cs
    Get-ChildItem (Join-Path $repo 'LicenseScope.App') -Recurse -File -Include *.cs,*.xaml
) | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$kmsRemediationPath = Join-Path $repo 'LicenseScope.App\KmsManagementService.cs'
$softwareUninstallPath = Join-Path $repo 'LicenseScope.App\SoftwareUninstallService.cs'
$productionSource = ($sourceFiles | Get-Content -Raw) -join "`n"
$readOnlyProductionSource = ($sourceFiles | Where-Object { $_.FullName -ne $kmsRemediationPath } | Get-Content -Raw) -join "`n"
$kmsRemediationSource = Get-Content -LiteralPath $kmsRemediationPath -Raw
$softwareUninstallSource = Get-Content -LiteralPath $softwareUninstallPath -Raw
$crackTraceSource = @(
    Get-Content -LiteralPath (Join-Path $repo 'src\LicenseScope.Windows\WindowsCrackTraceAnalyzer.cs') -Raw
    Get-Content -LiteralPath (Join-Path $repo 'src\LicenseScope.Windows\Acquisition\WindowsCrackTraceEvidenceSource.cs') -Raw
) -join "`n"
$kmsMutationCommands = @([regex]::Matches(
    $kmsRemediationSource,
    '(?i)/(ato|ipk|upk|cpky|rearm|skms|ckms)\b'
) | ForEach-Object { $_.Groups[1].Value })
$scannerIds = @('microsoft.windows','microsoft.office','autodesk.desktop','adobe.desktop','trimble.sketchup')
$checks = [ordered]@{
    'Exactly 20 projects' = $projectMatches.Count -eq 20
    'Unique project names' = @($names | Sort-Object -Unique).Count -eq 20
    'Unique project paths' = @($paths | Sort-Object -Unique).Count -eq 20
    'Unique project GUIDs' = @($guids | Sort-Object -Unique).Count -eq 20
    'All project paths exist' = $missing.Count -eq 0
    'Project GUID set preserved' = @($expectedGuids | Where-Object { $_ -notin $guids }).Count -eq 0
    'AnyCPU preserved' = $solution -match 'Any CPU'
    'x86 preserved' = $solution -match '\|x86'
    'x64 preserved' = $solution -match '\|x64'
    'No legacy WinLic technical branding' = @($oldBrandHits).Count -eq 0
    'Stable scanner IDs preserved' = @($scannerIds | Where-Object { $scannerSource -notmatch [regex]::Escape($_) }).Count -eq 0
    'No activation mutation commands outside KMS remediation' = $readOnlyProductionSource -notmatch '(?i)/(ato|ipk|upk|cpky|rearm|skms|ckms)\b'
    'KMS remediation is clear-only' = $kmsMutationCommands.Count -eq 1 -and $kmsMutationCommands[0] -eq 'ckms'
    'Software uninstall safety boundary preserved' =
        $softwareUninstallSource -match 'microsoft\.windows' -and
        $softwareUninstallSource -match 'BlockedExecutables' -and
        $softwareUninstallSource -match 'Verb\s*=\s*"runas"' -and
        $softwareUninstallSource -notmatch 'QuietUninstallString'
    'No production simulation surface' =
        $productionSource -notmatch '(?i)(SimulationAuditLoader|SimulationScan|SIMULATION)' -and
        @(Get-ChildItem -LiteralPath (Join-Path $repo 'LicenseScope.App\Samples') -Recurse -File -ErrorAction SilentlyContinue).Count -eq 0
    'Crack trace scanner remains read-only' =
        $crackTraceSource -notmatch '(?i)/(ato|ipk|upk|cpky|rearm|skms|ckms)\b' -and
        $crackTraceSource -notmatch '(?i)(RegistryKey\.SetValue|Registry\.SetValue|DeleteValue|DeleteSubKey|File\.Delete|Directory\.Delete|Stop-Service|schtasks(?:\.exe)?\s+/(delete|change|end|run))' -and
        $crackTraceSource -match 'Arguments\s*=\s*"/query /fo csv /v /nh"'
    'No telemetry or report upload' = $productionSource -notmatch '(?i)(telemetry|upload(report|audit)|HttpClient|WebClient)'
}
$failed = @($checks.GetEnumerator() | Where-Object { -not $_.Value })
if ($failed) { throw ('Rebranding policy failures: ' + (($failed.Name) -join ', ')) }
Write-Host "RebrandingPolicy: $($checks.Count) passed; projects=$($projectMatches.Count)"
