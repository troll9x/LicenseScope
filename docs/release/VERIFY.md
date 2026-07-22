# Verify this release candidate

Run `Get-FileHash -Algorithm SHA256 <file>` and compare every entry with `WinLic-1.0.0-SHA256SUMS.txt`. Review `WinLic-1.0.0-release-manifest.json` and the SPDX 2.2 SBOM. This unsigned RC must have a directory/name ending `rc.1-unsigned`; it is not a signed stable release.
