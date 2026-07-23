# Third-party notices

Production License Scope assemblies use .NET Framework system libraries and contain no NuGet runtime packages. Test projects use Microsoft.NET.Test.Sdk, MSTest.TestAdapter and MSTest.TestFramework under their upstream Microsoft license terms; transitive test/build dependencies are listed in `docs/security/dependency-inventory.md` and are not bundled in Setup.

The installer is built with Inno Setup; its executable is not committed. The Microsoft .NET Framework 4.8 offline redistributable is bundled subject to Microsoft redistribution terms. Microsoft SBOM Tool is a release-runner tool only and is neither committed nor redistributed.
