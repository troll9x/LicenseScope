using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinLic.Core.Runtime;

namespace WinLic.Core.Tests
{
    [TestClass]
    public sealed class ProcessRunnerTests
    {
        [TestMethod]
        public async Task MissingExecutableReturnsStructuredStartFailure()
        {
            var result = await new ProcessRunner().RunAsync(new ProcessExecutionRequest
            {
                ExecutablePath = @"Z:\fixture\does-not-exist.exe",
                Timeout = TimeSpan.FromSeconds(1)
            }, CancellationToken.None);
            Assert.IsTrue(result.StartFailure);
            Assert.IsNull(result.ExitCode);
            Assert.IsFalse(result.TimedOut);
        }

        [TestMethod]
        public async Task EmptyExecutablePathIsRejected()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => new ProcessRunner().RunAsync(new ProcessExecutionRequest(), CancellationToken.None));
        }
    }
}
