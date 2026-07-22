# Phase 10 official release research

Reviewed 2026-07-22: GitHub Actions secure-use/runner/permissions/environments/releases/attestation documentation, Microsoft SignTool, Microsoft SBOM Tool 4.1.5, NuGet lock files, .NET version properties and Inno Setup documentation.

Full action SHA is the immutable pin. CI uses `windows-2022`; images evolve despite the stable label. PR secrets and `pull_request_target` are prohibited. Attestation support, retention, protected environment, repository visibility/plan and tag existence are `NEEDS REMOTE VERIFICATION`. SignTool requires SHA-256 file/timestamp digests and `/pa` verification. SBOM is SPDX 2.2 with license lookup disabled. Lock files fail locked restore when dependency graphs change. Tags are `v1.0.0` or `v1.0.0-rc.1`; Phase 10 creates neither. Unsigned installers have SmartScreen/trust risk and remain RC-only.
