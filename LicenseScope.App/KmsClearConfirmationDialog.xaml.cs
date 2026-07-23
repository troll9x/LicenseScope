using System;
using System.Windows;
using System.Windows.Controls;

namespace LicenseScope.App
{
    public partial class KmsClearConfirmationDialog : Window
    {
        public KmsClearConfirmationDialog(string currentSettings)
        {
            InitializeComponent();
            CurrentSettingsText.Text = currentSettings ?? string.Empty;
        }

        private void ConfirmationTextBox_Changed(object sender, TextChangedEventArgs e)
        {
            ConfirmButton.IsEnabled = string.Equals(
                ConfirmationTextBox.Text.Trim(),
                "XOA KMS",
                StringComparison.OrdinalIgnoreCase);
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
