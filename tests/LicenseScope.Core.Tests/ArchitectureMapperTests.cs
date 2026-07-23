using Microsoft.VisualStudio.TestTools.UnitTesting;
using LicenseScope.Core.Models;
using LicenseScope.Core.Runtime;

namespace LicenseScope.Core.Tests
{
    [TestClass]
    public sealed class ArchitectureMapperTests
    {
        [DataTestMethod]
        [DataRow("x86", OperatingSystemArchitecture.X86)]
        [DataRow("AMD64", OperatingSystemArchitecture.X64)]
        [DataRow("ARM", OperatingSystemArchitecture.Arm32)]
        [DataRow("ARM64", OperatingSystemArchitecture.Arm64)]
        [DataRow("unexpected", OperatingSystemArchitecture.Unknown)]
        public void MapsOperatingSystemLabels(string label, OperatingSystemArchitecture expected) => Assert.AreEqual(expected, ArchitectureMapper.MapOperatingSystem(label, false));

        [DataTestMethod]
        [DataRow("x86", ProcessArchitecture.X86)]
        [DataRow("AMD64", ProcessArchitecture.X64)]
        [DataRow("ARM", ProcessArchitecture.Arm32)]
        [DataRow("ARM64", ProcessArchitecture.Arm64)]
        [DataRow("unexpected", ProcessArchitecture.Unknown)]
        public void MapsProcessLabels(string label, ProcessArchitecture expected) => Assert.AreEqual(expected, ArchitectureMapper.MapProcess(label, false));
    }
}
