using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace WinLicApp
{
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            Title          = L.Get("About_Title");
            TxtVersion.Text   = L.Get("About_Version");
            TxtLblAuthor.Text = L.Get("About_Author");
            TxtLblGitHub.Text = L.Get("About_GitHub");
            TxtLblRepo.Text   = L.Get("About_Repo");
            TxtDesc.Text      = L.Get("About_Desc");
            BtnClose.Content  = L.Get("About_Close");
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
