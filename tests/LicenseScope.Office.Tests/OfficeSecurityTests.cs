using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Runtime;
using LicenseScope.Office.Acquisition;
using LicenseScope.Office.Models;

namespace LicenseScope.Office.Tests
{
    [TestClass] public sealed class OfficeSecurityTests
    {
        [TestMethod] public void OutputSanitizerMasksKeyEmailAndIdentifier() { var value = OfficeOutputSanitizer.Sanitize("AAAAA-BBBBB-CCCCC-DDDDD-EEEEE person@example.com 11111111-2222-3333-4444-555555555555"); Assert.IsFalse(value.Contains("AAAAA-BBBBB")); Assert.IsFalse(value.Contains("person@example.com")); Assert.IsFalse(value.Contains("11111111-2222")); StringAssert.Contains(value, "EEEEE"); }
        [TestMethod] public async Task OsppAllowlistAcceptsOnlyReadOnlyOptions()
        {
            var file = Path.GetTempFileName(); try { var runner = new FakeRunner(); var provider = new OsppEvidenceProvider(runner); var tool = new OfficeToolLocation { ToolType = "OSPP", FullPath = file }; Assert.IsTrue((await provider.RunFixedAsync(tool, "/dstatus", CancellationToken.None)).Success); Assert.IsTrue((await provider.RunFixedAsync(tool, "/dstatusall", CancellationToken.None)).Success); var names = new[] { "act", "inpkey", "rearm", "sethst" }; foreach (var name in names) await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => provider.RunFixedAsync(tool, "/" + name, CancellationToken.None)); } finally { File.Delete(file); }
        }
        [TestMethod] public async Task MissingOfficialToolIsWarningNotUnlicensed() { var result = await new OsppEvidenceProvider(new FakeRunner()).ReadStatusAsync(new OfficeToolLocation { ToolType = "OSPP", FullPath = @"Z:\missing\ospp.vbs" }, CancellationToken.None); Assert.IsFalse(result.Success); StringAssert.Contains(result.Warning, "not found"); }
        private sealed class FakeRunner : IProcessRunner { public List<ProcessExecutionRequest> Requests { get; } = new List<ProcessExecutionRequest>(); public Task<ProcessExecutionResult> RunAsync(ProcessExecutionRequest request, CancellationToken token) { Requests.Add(request); return Task.FromResult(new ProcessExecutionResult { ExitCode = 0, StandardOutput = "LICENSE NAME: OfficeRetail\nLICENSE STATUS: LICENSED" }); } }
    }
}
