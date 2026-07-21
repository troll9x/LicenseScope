# Build matrix

Run `.\build\Build-CompatibilityMatrix.ps1 -Configuration Release -OutputRoot .\artifacts\compatibility`. It restores, runs the full standard test suite, builds x86/x64/AnyCPU explicitly, PE-inspects CLI and generates SHA-256 in ignored artifacts. Architecture-specific MSTest execution is a recorded test-host blocker; validate production payloads with smoke tests. ARM64 remains blocked. Never use `-SkipTests` for release verification or commit generated output.
