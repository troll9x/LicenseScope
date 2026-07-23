# Offline .NET Framework prerequisite

Place the Microsoft-signed .NET Framework 4.8 offline runtime named
`NDP48-x86-x64-AllOS-ENU.exe` in `cache/`. The cache is ignored by Git.

`Build-Installer.ps1` verifies its exact byte length, SHA-256, version metadata,
Authenticode signature and Microsoft signer before staging it. It never downloads
or executes the package. Setup invokes it only when the installed .NET Framework
release key is below `528040`, using `/q /norestart /ChainingPackage LicenseScope`.

The package must not be substituted with the web installer, Developer Pack, or
Targeting Pack. Redistribution remains subject to Microsoft's license terms.
