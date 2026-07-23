using System;
using System.Linq;
using System.Windows;
using LicenseScope.Compatibility;

namespace LicenseScope.App
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var environment = new RuntimeEnvironmentDetector(new WindowsArchitectureProbe()).Detect();
            var assessment = new CompatibilityEvaluator().Evaluate(environment, CurrentPayload.Describe(environment));
            if (!assessment.CanStart)
            {
                MessageBox.Show("Phiên bản Windows hoặc kiến trúc này không được payload hiện tại hỗ trợ.\n" +
                    "This Windows version or architecture is not supported by the current payload.\n\n" +
                    string.Join("\n", assessment.BlockingReasons) + "\n\nKhông có thao tác bản quyền nào đã được thực hiện.",
                    "License Scope compatibility", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(7); return;
            }
            if (assessment.IsEndOfLife)
                MessageBox.Show("Cảnh báo: Hệ điều hành này đã hết hỗ trợ bảo mật. License Scope chỉ thực hiện kiểm tra read-only.\n\n" +
                    "Warning: This operating system is end of life. License Scope performs read-only auditing only.",
                    "License Scope compatibility", MessageBoxButton.OK, MessageBoxImage.Warning);
            base.OnStartup(e);
        }
    }
}
