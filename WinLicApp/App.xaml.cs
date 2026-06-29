using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;

namespace WinLicApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // .NET Framework 4.8 is pre-installed on Windows 10 (1903+) and Windows 11.
            // On very old Win10 builds it may be absent — detect and guide the user.
            if (!IsDotNetFramework48Installed())
            {
                var result = MessageBox.Show(
                    "WinLic Manager yêu cầu .NET Framework 4.8 hoặc cao hơn.\n" +
                    ".NET Framework 4.8 is required to run WinLic Manager.\n\n" +
                    "Phiên bản này chưa được cài đặt trên máy tính của bạn.\n" +
                    "This version is not installed on your machine.\n\n" +
                    "Nhấn OK để mở trang tải xuống của Microsoft.\n" +
                    "Click OK to open the Microsoft download page.",
                    "WinLic Manager — .NET Framework 4.8 Required",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.OK)
                {
                    Process.Start(new ProcessStartInfo(
                        "https://dotnet.microsoft.com/download/dotnet-framework/net48")
                    { UseShellExecute = true });
                }

                Shutdown(1);
                return;
            }

            base.OnStartup(e);
        }

        /// <summary>
        /// Checks HKLM for .NET Framework 4.8 (Release >= 528040).
        /// See: https://learn.microsoft.com/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
        /// </summary>
        private static bool IsDotNetFramework48Installed()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full");
                if (key == null) return false;
                var release = key.GetValue("Release") as int?;
                return release.HasValue && release.Value >= 528040; // 528040 = 4.8 RTM
            }
            catch
            {
                return false;
            }
        }
    }
}
