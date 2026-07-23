using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;
using LicenseScope.Core.Runtime;
using LicenseScope.Core.Services;
using LicenseScope.Windows;
using LicenseScope.Office;
using LicenseScope.Application;

namespace LicenseScope.App
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
        public static WindowsAuditServices CreateWindowsAudit(AuditSettings? settings = null)
        {
            settings = settings ?? AuditSettings.Load();
            var clock = new SystemClock();
            var scanner = WindowsScannerFactory.Create(
                new ProcessRunner(),
                clock,
                new ConfigurableWindowsKnownKeyCatalog(settings.ExtraGenericKeys),
                settings.ToWindowsInspectionSettings());
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

        public static IUnifiedAuditService CreateUnifiedAudit(AuditSettings? settings = null)
        {
            settings = settings ?? AuditSettings.Load();
            return UnifiedAuditService.CreateProduction(
                new ConfigurableWindowsKnownKeyCatalog(settings.ExtraGenericKeys),
                settings.ToWindowsInspectionSettings());
        }

        public static ICrackTraceAnalyzer CreateCrackTraceAnalyzer()
        {
            return CrackTraceAnalyzerFactory.Create(new ProcessRunner());
        }
    }
}
