using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LicenseScope.App
{
    public partial class AuditSettingsDialog : Window
    {
        private readonly AuditSettings _settings;

        public AuditSettingsDialog()
        {
            InitializeComponent();
            _settings = AuditSettings.Load();
            Populate();
        }

        private void Populate()
        {
            EnableInspectionCheckBox.IsChecked = _settings.EnableExtendedInspection;
            DefaultPortsTextBox.Text = Lines(AuditSettings.DefaultPorts);
            DefaultServicesTextBox.Text = Lines(AuditSettings.DefaultServices);
            DefaultTasksTextBox.Text = Lines(AuditSettings.DefaultTaskKeywords);
            DefaultProcessesTextBox.Text = Lines(AuditSettings.DefaultProcesses);
            DefaultDomainsTextBox.Text = Lines(AuditSettings.DefaultKmsDomains);
            DefaultPathsTextBox.Text = Lines(AuditSettings.DefaultFilePaths);

            ExtraKeysTextBox.Text = Lines(_settings.ExtraGenericKeys
                .OrderBy(x => x.Key).Select(x => x.Key + " = " + x.Value));
            ExtraPortsTextBox.Text = Lines(_settings.ExtraPorts);
            ExtraServicesTextBox.Text = Lines(_settings.ExtraServices);
            ExtraTasksTextBox.Text = Lines(_settings.ExtraTaskKeywords);
            ExtraProcessesTextBox.Text = Lines(_settings.ExtraProcesses);
            ExtraDomainsTextBox.Text = Lines(_settings.ExtraKmsDomains);
            ExtraPathsTextBox.Text = Lines(_settings.ExtraFilePaths);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _settings.EnableExtendedInspection = EnableInspectionCheckBox.IsChecked == true;
            _settings.ExtraGenericKeys = ParseKeys(ExtraKeysTextBox.Text);
            _settings.ExtraPorts = ParseLines(ExtraPortsTextBox.Text)
                .Select(x => { int port; return int.TryParse(x, out port) ? port : -1; })
                .Where(x => x > 0 && x <= 65535).Distinct().ToList();
            _settings.ExtraServices = ParseLines(ExtraServicesTextBox.Text);
            _settings.ExtraTaskKeywords = ParseLines(ExtraTasksTextBox.Text);
            _settings.ExtraProcesses = ParseLines(ExtraProcessesTextBox.Text);
            _settings.ExtraKmsDomains = ParseLines(ExtraDomainsTextBox.Text);
            _settings.ExtraFilePaths = ParseLines(ExtraPathsTextBox.Text);
            try
            {
                _settings.Save();
                DialogResult = true;
                Close();
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this, "Không thể lưu cài đặt: " + ex.Message,
                    "Cài đặt kiểm tra", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearCustom_Click(object sender, RoutedEventArgs e)
        {
            ExtraKeysTextBox.Clear();
            ExtraPortsTextBox.Clear();
            ExtraServicesTextBox.Clear();
            ExtraTasksTextBox.Clear();
            ExtraProcessesTextBox.Clear();
            ExtraDomainsTextBox.Clear();
            ExtraPathsTextBox.Clear();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static Dictionary<string, string> ParseKeys(string text)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in ParseLines(text))
            {
                var parts = line.Split(new[] { '=' }, 2);
                var suffix = AuditSettings.ExtractSuffix(parts[0]);
                if (suffix.Length != 5) continue;
                values[suffix] = parts.Length == 2 && parts[1].Trim().Length > 0
                    ? parts[1].Trim()
                    : "Khóa bổ sung";
            }
            return values;
        }

        private static List<string> ParseLines(string text)
        {
            return (text ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0 && !x.StartsWith(";") && !x.StartsWith("#"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string Lines<T>(IEnumerable<T> values) =>
            string.Join(Environment.NewLine, values);
    }
}
