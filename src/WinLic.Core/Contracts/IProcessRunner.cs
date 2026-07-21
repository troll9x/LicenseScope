using System.Threading;
using System.Threading.Tasks;
using WinLic.Core.Runtime;

namespace WinLic.Core.Contracts
{
    /// <summary>Runs a process without invoking a shell and captures its outcome.</summary>
    public interface IProcessRunner
    {
        Task<ProcessExecutionResult> RunAsync(ProcessExecutionRequest request, CancellationToken cancellationToken);
    }
}
