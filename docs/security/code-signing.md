# Code-signing policy

Stable public releases require a valid Authenticode Code Signing EKU, `/fd SHA256`, RFC 3161 `/tr` timestamp with `/td SHA256`, valid chain and successful `signtool verify /pa /tw`. Certificates come from the Windows store or a hardware provider; PFX/passwords are forbidden in Git.

Unsigned RCs must end `-unsigned`, remain prerelease/draft, include hashes/SBOM and clearly warn users. They cannot be stable or submitted to WinGet. Current status: `BLOCKED_NO_CERTIFICATE`.
