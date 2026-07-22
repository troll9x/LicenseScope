# Contributing

Create a focused branch and run `.\build\Invoke-CI.ps1 -Configuration All -Platforms AnyCPU,x86,x64`. Scanner changes must remain read-only, isolate errors, mask sensitive values and include synthetic fixtures. Never commit real keys, vendor output/tools, credentials, binaries or reports; never add activation or remediation.

Package changes must intentionally regenerate and review `packages.lock.json`. Run installer policy tests after installer changes. Build an RC with `Build-ReleaseCandidate.ps1` and a pre-provisioned Microsoft SBOM Tool. Add localization in both English and Vietnamese. Compatibility claims require recorded runtime evidence; absence of a crash is not evidence of support.
