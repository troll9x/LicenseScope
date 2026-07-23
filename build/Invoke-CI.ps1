[CmdletBinding()]param([ValidateSet('All','Debug','Release')][string]$Configuration='All',[string[]]$Platforms=@('AnyCPU','x86','x64'))
$ErrorActionPreference='Stop';$repo=Split-Path $PSScriptRoot -Parent;$sln=Join-Path $repo 'LicenseScope.sln'
function Run([scriptblock]$Command,[string]$Name){& $Command;if($LASTEXITCODE-ne 0){throw "$Name failed ($LASTEXITCODE)."}}
Run {dotnet restore $sln --locked-mode --verbosity minimal} 'locked restore'
$configs=if($Configuration-eq'All'){@('Debug','Release')}else{@($Configuration)}
foreach($c in $configs){Run {dotnet build $sln -c $c --no-restore --verbosity minimal} "$c build";Run {dotnet test $sln -c $c --no-build --logger 'console;verbosity=minimal'} "$c tests"}
foreach($p in $Platforms){if($p-ne'AnyCPU'){Run {dotnet build $sln -c Release --no-restore --verbosity minimal /p:PlatformTarget=$p /p:Prefer32Bit=false} "$p build"}}
Run {powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo 'installer\tests\InstallerPolicy.Tests.ps1')} 'installer policy'
Run {powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo 'installer\tests\InstallerManifest.Tests.ps1')} 'prerequisite manifest'
Run {powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo 'installer\tests\InstallerCoexistence.Tests.ps1')} 'installer coexistence policy'
Run {powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo 'tests\release\ReleasePolicy.Tests.ps1')} 'release policy'
Run {powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo 'tests\rebranding\RebrandingPolicy.Tests.ps1')} 'rebranding policy'
$files=@(Get-ChildItem (Join-Path $repo 'build') -Filter *.ps1)+@(Get-ChildItem (Join-Path $repo 'installer\tests') -Filter *.ps1)+@(Get-ChildItem (Join-Path $repo 'tests\rebranding') -Filter *.ps1)+@(Get-ChildItem (Join-Path $repo 'tests\release') -Filter *.ps1)+@(Get-ChildItem (Join-Path $repo 'tools\compatibility') -Filter *.ps1)
$errors=0;foreach($f in $files){$t=$null;$e=$null;[Management.Automation.Language.Parser]::ParseFile($f.FullName,[ref]$t,[ref]$e)|Out-Null;$errors+=$e.Count};if($errors){throw "$errors PowerShell parser error(s)."}
git -C $repo diff --check;if($LASTEXITCODE){throw 'git diff --check failed.'}
Write-Host "CI PASS configurations=$($configs-join ',') platforms=$($Platforms-join ',') parserFiles=$($files.Count)"
