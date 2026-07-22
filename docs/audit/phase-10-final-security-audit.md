# Final security/privacy audit

| Severity | Evidence | Impact | Decision / regression control |
|---|---|---|---|
| High | Legacy administration handlers in `MainWindow.xaml.cs` can change licensing state | Misuse outside audit | Deferred legacy surface; never invoked by Scan All or installer; installer/release policy scans forbid activation commands |
| Medium | Production RC lacks Authenticode certificate | Publisher warning/SmartScreen | Unsigned prerelease only; stable gate rejects; signing scripts require trusted store certificate |
| Medium | Self-hosted release runner/environment not remotely verified | Supply-chain exposure if misconfigured | Release workflow cannot run on PR and publish defaults false; `NEEDS REMOTE VERIFICATION` |
| Low | Existing nullable/analyzer warnings | Potential robustness debt | Existing tests and scanner isolation; defer without changing scanner policy |

No telemetry/upload SDK is in production. Network usage is limited to the explicit legacy settings update, not unified audit; no default report upload exists. Scanner acquisition is read-only and masks evidence. No private keys, certificate files or signing passwords are tracked.
