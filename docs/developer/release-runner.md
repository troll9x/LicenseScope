# Protected release runner

Required labels: `self-hosted`, `windows`, `x64`, `licensescope-release`. Isolate it from PRs, restrict it to the protected `production-release` environment, use required reviewers and clean workspaces. Preinstall .NET SDK/MSBuild, net48 targeting pack, Inno Setup 7.0.2, offline framework cache and Microsoft SBOM Tool 4.1.5. Optional SignTool/certificate must not be exposed to untrusted jobs. Configuration status: `NEEDS REMOTE VERIFICATION`.
