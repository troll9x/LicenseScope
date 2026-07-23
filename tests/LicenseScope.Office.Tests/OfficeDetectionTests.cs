using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LicenseScope.Office.Detection;
using LicenseScope.Office.Models;

namespace LicenseScope.Office.Tests
{
    [TestClass] public sealed class OfficeDetectionTests
    {
        [TestMethod] public void SubscriptionAndVolumeProjectUseDifferentMechanisms()
        {
            Assert.IsTrue(OfficeInstallationDetector.FromId("ProjectProRetail", "", "x86", "ClickToRun", "", true).UsesVNext);
            Assert.IsFalse(OfficeInstallationDetector.FromId("ProjectPro2021Volume", "", "x64", "ClickToRun", "", true).UsesVNext);
        }
        [TestMethod] public void ToolLocatorFindsRootOffice16AndOrdersDeterministically()
        {
            var root = Path.Combine(Path.GetTempPath(), "winlic-office-locator-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(Path.Combine(root, "root", "Office16"));
            try { File.WriteAllText(Path.Combine(root, "root", "Office16", "OSPP.VBS"), "synthetic"); File.WriteAllText(Path.Combine(root, "root", "Office16", "vnextdiag.ps1"), "synthetic"); var tools = new OfficeToolLocator().Locate(new[] { new OfficeInstallation { RootPath = root, Architecture = "x86" } }); Assert.AreEqual(2, tools.Count); Assert.AreEqual("OSPP", tools[0].ToolType); Assert.AreEqual("x86", tools[0].Architecture); }
            finally { Directory.Delete(root, true); }
        }
        [TestMethod] public void ToolLocatorMissingPathReturnsEmpty() { Assert.AreEqual(0, new OfficeToolLocator().Locate(new[] { new OfficeInstallation { RootPath = @"Z:\missing-office" } }).Count); }
    }
}
