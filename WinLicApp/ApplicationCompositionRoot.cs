using WinLic.Core.Contracts;
using WinLic.Core.Models;
using WinLic.Core.Runtime;
using WinLic.Core.Services;
using WinLic.Scanners.Windows;

namespace WinLicApp
{
    internal sealed class WindowsAuditServices
    {
        public IAuditOrchestrator Orchestrator { get; set; } = null!;
        public SystemContext Context { get; set; } = null!;
    }

    internal static class ApplicationCompositionRoot
    {
        public static WindowsAuditServices CreateWindowsAudit()
        {
            var clock = new SystemClock();
            var scanner = WindowsScannerFactory.Create(new ProcessRunner(), clock);
            return new WindowsAuditServices
            {
                Orchestrator = new AuditOrchestrator(new[] { scanner }, clock),
                Context = new DefaultSystemContextProvider().GetCurrent()
            };
        }
    }
}
