using System;
using System.Linq;
using System.Windows;
using WinLic.Compatibility;

namespace WinLicApp
{
    public partial class App : Application
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
                    "WinLic compatibility", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(7); return;
            }
            if (assessment.IsEndOfLife)
                MessageBox.Show("Cảnh báo: Hệ điều hành này đã hết hỗ trợ bảo mật. WinLic chỉ thực hiện kiểm tra read-only.\n\n" +
                    "Warning: This operating system is end of life. WinLic performs read-only auditing only.",
                    "WinLic compatibility", MessageBoxButton.OK, MessageBoxImage.Warning);
            base.OnStartup(e);
        }
    }
}
