# Release process

Use a reviewed commit and protected `production-release` environment on the `[self-hosted, windows, x64, winlic-release]` runner. Configure required reviewers and branch/tag restrictions (`NEEDS REMOTE VERIFICATION`). Pre-provision .NET/targeting packs, Inno 7.0.2, the verified offline framework cache and Microsoft SBOM Tool 4.1.5. Signing additionally needs Windows SDK SignTool and a hardware/store-backed certificate.

Run local CI, then `Build-ReleaseCandidate.ps1`. Unsigned output is prerelease-only. Stable publishing requires a valid signature and exact `v1.0.0` tag/version; publishing remains disabled in the workflow pending remote review. Never reuse an asset name with different bytes.
