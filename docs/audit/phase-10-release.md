# Phase 10 release hardening

Version 1.0.0 is single-source in `Directory.Build.props`. CI, release policy, locks, SBOM/signing scripts, manifest/checksums, bundle verification and release documentation are source controlled. The local candidate is unsigned and prerelease-only. Remote Actions, protected environment and artifact attestations remain not run/verified because no push, tag or release is authorized.

## Local release-candidate evidence

- Bundle: `artifacts/release/WinLic-1.0.0-rc.1-unsigned` (ignored by Git), containing exactly Setup, SPDX SBOM, release manifest, checksum list, release notes and verification guide.
- Setup SHA-256: `41A5FAE14955A862FF0E57D93E28E847B251C5B56E90EFC8C0D9B33AA74A60A2`; Authenticode state: `NotSigned`.
- SBOM SHA-256: `83AC631666370C83D4CA3D3F8BCCAFDF71F581A913B53A508B7D626F2DA843CA`; SPDX 2.2; Setup checksum matched; Microsoft SBOM validation reported one successful file and zero failures/additional/missing files.
- SBOM generator: externally supplied Microsoft.Sbom.Tool 4.1.5, signed by Microsoft Corporation, SHA-256 `625767B371B7FDD58F40F618B8A86DA0247A33C89E419039C86B4EDBA1DAD4B5`. The executable was not copied into the repository or release bundle. External license lookup was disabled; packet-level network monitoring was not performed.
- Local CI: Debug and Release builds passed with zero warnings/errors; 296 tests passed in each configuration; AnyCPU/x86/x64 build gates, 12 installer-policy checks, 9 installer-manifest checks and 16 release-policy checks passed.
- Production smoke on the current x64 host: elevated install exit 0, payload x64, .NET prerequisite not invoked, CLI audit exit 0 with JSON/CSV/HTML, GUI startup passed, elevated uninstall exit 0, and no installation files or uninstall entry remained.

The candidate was built from the uncommitted Phase 10 working tree only because the local verification command explicitly used the developer-only `AllowDirtySource` switch. The release workflow cannot pass that switch and requires committed source. No tag, push, GitHub artifact, attestation or release was created.
