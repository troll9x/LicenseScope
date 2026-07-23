using System.Windows;
using System.Windows.Input;

namespace LicenseScope.App
{
    public partial class InputDialog : Window
    {
        public string InputValue => InputBox.Text;

        public InputDialog(string prompt, string title)
        {
            InitializeComponent();
            Title           = title;
            PromptText.Text = prompt;

            // Localise OK / Cancel based on the active language
            OkButton.Content     = L.Get("Dialog_OK");
            CancelButton.Content = L.Get("Dialog_Cancel");

            Loaded += (_, _) => InputBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)     => DialogResult = true;
        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)  DialogResult = true;
            if (e.Key == Key.Escape) DialogResult = false;
        }
    }
}
