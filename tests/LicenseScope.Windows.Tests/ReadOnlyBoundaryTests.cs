using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Runtime;
using LicenseScope.Windows.Acquisition;
using LicenseScope.Windows.Parsing;

namespace LicenseScope.Windows.Tests
{
    [TestClass]
    public sealed class ReadOnlyBoundaryTests
    {
        [DataTestMethod]
        [DataRow("/xpr")]
        [DataRow("/dlv")]
        public async Task AllowsOnlyDocumentedReadOnlyOptions(string option)
        {
            var runner = new CapturingRunner();
            var provider = new WindowsSlmgrEvidenceProvider(runner, new SlmgrXprParser(), new SlmgrDlvParser());
            var result = await provider.RunFixedAsync("cscript.exe", "slmgr.vbs", option, CancellationToken.None);
            Assert.IsTrue(result.Success);
            StringAssert.EndsWith(runner.Requests[0].Arguments, option);
        }

        [TestMethod]
        public async Task RejectsEveryOtherSlmgrOption()
        {
            var provider = new WindowsSlmgrEvidenceProvider(new CapturingRunner(), new SlmgrXprParser(), new SlmgrDlvParser());
            var stateChangingOptionNames = new[] { "ato", "ipk", "rearm" };
            foreach (var optionName in stateChangingOptionNames)
            {
                var option = "/" + optionName;
                await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => provider.RunFixedAsync("cscript.exe", "slmgr.vbs", option, CancellationToken.None));
            }

            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => provider.RunFixedAsync("cscript.exe", "slmgr.vbs", string.Empty, CancellationToken.None));
        }

        [TestMethod]
        public void WmiServiceRejectsNonSelectQuery()
        {
            Assert.ThrowsException<ArgumentException>(() => new WindowsWmiQueryService().Query("DELETE FROM SoftwareLicensingProduct"));
        }

        private sealed class CapturingRunner : IProcessRunner
        {
            public List<ProcessExecutionRequest> Requests { get; } = new List<ProcessExecutionRequest>();
            public Task<ProcessExecutionResult> RunAsync(ProcessExecutionRequest request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                return Task.FromResult(new ProcessExecutionResult { ExitCode = 0, StandardOutput = "The machine is permanently activated." });
            }
        }
    }
}
