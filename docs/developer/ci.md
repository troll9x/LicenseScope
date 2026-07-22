# CI

Run `.\build\Invoke-CI.ps1 -Configuration All -Platforms AnyCPU,x86,x64`. It performs locked restore, builds/tests, x86/x64 builds, installer/release policy tests, PowerShell parsing and whitespace checks. GitHub CI uses `windows-2022`, no secrets and read-only repository access. Remote execution is not claimed until pushed.
