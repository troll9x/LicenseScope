using System;
using System.Windows;
using System.Windows.Controls;

namespace LicenseScope.App
{
    public partial class SoftwareUninstallConfirmationDialog : Window
    {
        public SoftwareUninstallConfirmationDialog(string scannedProduct, string registeredProduct)
        {
            InitializeComponent();
            ScannedProductText.Text = scannedProduct;
            RegisteredProductText.Text = registeredProduct;
        }

        private void ConfirmationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ConfirmButton.IsEnabled = string.Equals(
                ConfirmationTextBox.Text.Trim(),
                "GO PHAN MEM",
                StringComparison.Ordinal);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
