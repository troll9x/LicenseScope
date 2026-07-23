using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace LicenseScope.App
{
    public partial class AboutDialog : Window
    {
        public AboutDialog() => InitializeComponent();

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Repository_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
