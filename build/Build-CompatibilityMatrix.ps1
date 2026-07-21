[CmdletBinding()] param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$OutputRoot='.\artifacts\compatibility',[string[]]$Platforms=@('x86','x64','Any CPU'),[switch]$SkipTests)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')); $out=[IO.Path]::GetFullPath((Join-Path $repo $OutputRoot))
$allowed=[IO.Path]::GetFullPath((Join-Path $repo 'artifacts\compatibility'))
if(-not $out.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'OutputRoot must be under artifacts\compatibility.'}
if(Test-Path -LiteralPath $out){[IO.Directory]::Delete($out,$true)}; [IO.Directory]::CreateDirectory($out)|Out-Null
& dotnet --info | Out-Null; if($LASTEXITCODE){throw 'dotnet toolchain unavailable'}
& dotnet restore (Join-Path $repo 'WinLic.sln') --verbosity minimal; if($LASTEXITCODE){throw 'restore failed'}
$testStatus=if($SkipTests){'SKIPPED'}else{'STANDARD_ANYCPU_PASS; ARCHITECTURE_SPECIFIC_BLOCKED_BY_TESTHOST'}
if(-not $SkipTests){& dotnet test (Join-Path $repo 'WinLic.sln') -c $Configuration --logger 'console;verbosity=minimal';if($LASTEXITCODE){throw 'standard tests failed'}}
$results=@()
foreach($platform in $Platforms){
  if($platform -eq 'ARM64'){$results += [pscustomobject]@{platform='ARM64';status='BLOCKED';reason='No net481 ARM64 host is configured'};continue}
  $target=if($platform -eq 'Any CPU'){'AnyCPU'}else{$platform}
  & dotnet clean (Join-Path $repo 'WinLic.sln') -c $Configuration -p:Platform=$platform -p:PlatformTarget=$target --verbosity quiet; if($LASTEXITCODE){throw "clean failed: $platform"}
  & dotnet build (Join-Path $repo 'WinLic.sln') -c $Configuration -p:Platform=$platform -p:PlatformTarget=$target --no-restore --verbosity minimal; if($LASTEXITCODE){throw "build failed: $platform"}
  $id=if($platform -eq 'x86'){'winlic-net48-x86'}elseif($platform -eq 'x64'){'winlic-net48-x64'}else{'winlic-net48-anycpu'}
  $dest=Join-Path $out $id;[IO.Directory]::CreateDirectory($dest)|Out-Null
  foreach($source in @((Join-Path $repo "WinLicApp\bin\$Configuration\net48"),(Join-Path $repo "src\WinLic.Cli\bin\$Configuration\net48"))){Get-ChildItem -LiteralPath $source -File|Copy-Item -Destination $dest}
  $exe=Join-Path $dest 'WinLicAudit.Cli.exe';$pe=& (Join-Path $PSScriptRoot 'Get-PEArchitecture.ps1') -Path $exe
  $results += [pscustomobject]@{platform=$platform;payload=$id;status='PASS';tests=$testStatus;output=$dest;pe=$pe;sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $exe).Hash}
}
$results|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $out 'build-matrix-result.json') -Encoding UTF8
Get-ChildItem -LiteralPath $out -Recurse -File -Include *.exe,*.dll|ForEach-Object{"$((Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash) *$($_.FullName.Substring($out.Length+1))"}|Set-Content -LiteralPath (Join-Path $out 'checksums.sha256') -Encoding ASCII
$results|Format-Table -AutoSize
