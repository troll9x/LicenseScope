# Dependency inventory (reviewed 2026-07-22)

Production projects have no NuGet packages; only .NET Framework system references are bundled. Test projects directly use Microsoft.NET.Test.Sdk (17.11.1, 17.14.1, 18.0.1), MSTest.TestAdapter and MSTest.TestFramework (3.6.4, 3.10.4, 4.0.1). They transitively restore Microsoft test platform/code coverage, Application Insights test infrastructure, System.Buffers, Collections.Immutable, DiagnosticSource, Memory, Numerics.Vectors, Reflection.Metadata, Unsafe and Tasks.Extensions versions recorded in per-project lock files.

All are build/test-only and excluded from Setup. License source is upstream Microsoft/NuGet package metadata; no license enrichment is enabled during SBOM generation. Dependabot proposes weekly updates without auto-merge; major/test-generation changes are reviewed separately.
