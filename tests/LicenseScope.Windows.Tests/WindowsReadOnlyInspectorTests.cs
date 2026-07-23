using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;
using LicenseScope.Core.Runtime;
using LicenseScope.Windows.Acquisition;
using LicenseScope.Windows.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LicenseScope.Windows.Tests
{
    [TestClass]
    public sealed class WindowsReadOnlyInspectorTests
    {
        [TestMethod]
        public async Task ConfiguredTaskKeywordCreatesWarningUsingQueryOnly()
        {
            var runner = new FakeRunner
            {
                Result = new ProcessExecutionResult { ExitCode = 0, StandardOutput = "\"\\AutoKMS\"" }
            };
            var inspector = new WindowsReadOnlyInspector(runner, new WindowsInspectionSettings
            {
                TaskKeywords = new[] { "AutoKMS" }
            });

            var result = await inspector.InspectAsync(
                new SystemContext { WindowsDirectory = @"C:\Windows" },
                null,
                CancellationToken.None);

            Assert.AreEqual(1, runner.Requests.Count);
            Assert.AreEqual("/query /fo csv /nh", runner.Requests[0].Arguments);
            Assert.IsTrue(result.Warnings.Any(x => x.Contains("AutoKMS")));
        }

        [TestMethod]
        public async Task ConfiguredKmsDomainCreatesConservativeWarning()
        {
            var runner = new FakeRunner();
            var inspector = new WindowsReadOnlyInspector(runner, new WindowsInspectionSettings
            {
                KmsDomainKeywords = new[] { "example.test" }
            });
            var product = new WindowsLicenseProductRecord { KmsMachineName = "kms.example.test" };

            var result = await inspector.InspectAsync(
                new SystemContext { WindowsDirectory = @"C:\Windows" },
                product,
                CancellationToken.None);

            Assert.IsTrue(result.Warnings.Any(x => x.Contains("example.test")));
            Assert.AreEqual(0, runner.Requests.Count);
        }

        private sealed class FakeRunner : IProcessRunner
        {
            public List<ProcessExecutionRequest> Requests { get; } = new List<ProcessExecutionRequest>();
            public ProcessExecutionResult Result { get; set; } = new ProcessExecutionResult { ExitCode = 0 };

            public Task<ProcessExecutionResult> RunAsync(
                ProcessExecutionRequest request,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Requests.Add(request);
                return Task.FromResult(Result);
            }
        }
    }
}
