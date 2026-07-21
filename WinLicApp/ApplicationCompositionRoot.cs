using WinLic.Core.Contracts;
using WinLic.Core.Models;
using WinLic.Core.Runtime;
using WinLic.Core.Services;
using WinLic.Scanners.Windows;
using WinLic.Scanners.Office;
using WinLic.Application;

namespace WinLicApp
{
    internal sealed class WindowsAuditServices
    {
        public IAuditOrchestrator Orchestrator { get; set; } = null!;
        public SystemContext Context { get; set; } = null!;
    }

    internal sealed class OfficeAuditServices
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

        public static OfficeAuditServices CreateOfficeAudit()
        {
            var clock = new SystemClock();
            var scanner = OfficeScannerFactory.Create(new ProcessRunner());
            return new OfficeAuditServices
            {
                Orchestrator = new AuditOrchestrator(new[] { scanner }, clock),
                Context = new DefaultSystemContextProvider().GetCurrent()
            };
        }

        public static IUnifiedAuditService CreateUnifiedAudit() => UnifiedAuditService.CreateProduction();
    }
}
