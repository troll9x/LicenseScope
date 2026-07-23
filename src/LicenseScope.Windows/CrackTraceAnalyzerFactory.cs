using LicenseScope.Core.Contracts;
using LicenseScope.Core.Runtime;
using LicenseScope.Windows.Acquisition;

namespace LicenseScope.Windows
{
    public static class CrackTraceAnalyzerFactory
    {
        public static ICrackTraceAnalyzer Create(IProcessRunner processRunner)
        {
            return new WindowsCrackTraceAnalyzer(
                new WindowsCrackTraceEvidenceSource(processRunner));
        }

        public static ICrackTraceAnalyzer CreateDefault()
        {
            return Create(new ProcessRunner());
        }
    }
}
