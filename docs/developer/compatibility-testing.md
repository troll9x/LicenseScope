# Compatibility testing

Use the real OS/VM; never spoof version or use compatibility mode. Record payload SHA-256, sanitized compatibility output, GUI/CLI/audit/report results, Registry views, WMI/services/helpers, UAC and process architecture. Never record machine/account/license/server data. Use `tools/compatibility/Run-CompatibilityProbe.ps1`, then update the OS matrix with separate build/runtime/GUI/CLI/scanner evidence.
