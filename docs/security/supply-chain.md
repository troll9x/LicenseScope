# Supply-chain policy

CI uses `windows-2022`, read-only permissions and actions pinned to full commit SHA. PR builds receive no release secrets. Release builds use a protected self-hosted runner and environment; settings are `NEEDS REMOTE VERIFICATION`. The runner must pre-provision tools and must not download executables during release.

Attestation readiness requires job-scoped `contents: read`, `id-token: write` and `attestations: write` only after signing, final hashes, SBOM and manifest. It is not enabled until repository support and protected publishing are remotely verified.
