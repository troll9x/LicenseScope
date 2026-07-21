# Compatibility probe

Run `Run-CompatibilityProbe.ps1 -CliPath <payload>\WinLicAudit.Cli.exe` on the exact Windows/architecture under test. It performs only local read-only compatibility/audit commands, writes temporary sanitized reports, and removes them by default. Do not use compatibility mode, OS-version spoofing, elevated credentials, or production license data. Record the OS edition/build, native and process architectures, payload SHA-256, result, and test date in the Phase 8 matrix without recording machine name.
