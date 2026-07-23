using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Models;

namespace LicenseScope.Core.Contracts
{
    public interface ICrackTraceAnalyzer
    {
        Task<CrackTraceAnalysisResult> AnalyzeAsync(
            SystemContext context,
            CancellationToken cancellationToken);
    }
}
