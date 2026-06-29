using System;
using System.Linq;
using System.Windows;

namespace WinLicApp
{
    public partial class SettingsDialog : Window
    {
        public SettingsDialog()
        {
            InitializeComponent();
            ApplyLanguage();
            PopulateFields();
        }

        // ── Language ──────────────────────────────────────────────────────────────
        private void ApplyLanguage()
        {
            Title               = L.Get("SD_Title");
            TxtDialogTitle.Text = L.Get("SD_Title");
            TxtDialogDesc.Text  = L.Get("SD_Desc");

            GbPorts.Header    = L.Get("SD_Ports");
            GbServices.Header = L.Get("SD_Services");
            GbTasks.Header    = L.Get("SD_Tasks");
            GbProcs.Header    = L.Get("SD_Procs");
            GbFiles.Header    = L.Get("SD_Files");

            var defLabel   = L.Get("SD_Defaults");
            var custLabel  = L.Get("SD_Custom");
            TxtDefaultsLabel0.Text = defLabel; TxtCustomLabel0.Text = custLabel;
            TxtDefaultsLabel1.Text = defLabel; TxtCustomLabel1.Text = custLabel;
            TxtDefaultsLabel2.Text = defLabel; TxtCustomLabel2.Text = custLabel;
            TxtDefaultsLabel3.Text = defLabel; TxtCustomLabel3.Text = custLabel;

            TxtFilesNote.Text  = L.Get("SD_FilesNote");
            TxtSaveNote.Text   = L.Get("SD_SaveNote");
            BtnSave.Content    = L.Get("SD_Save");
            BtnCancel.Content  = L.Get("Dialog_Cancel");
        }

        // ── Populate with current values ──────────────────────────────────────────
        private void PopulateFields()
        {
            TxtDefaultPorts.Text    = string.Join(Environment.NewLine, AppSettings.DefaultPorts);
            TxtDefaultServices.Text = string.Join(Environment.NewLine, AppSettings.DefaultServices);
            TxtDefaultTasks.Text    = string.Join(Environment.NewLine, AppSettings.DefaultTaskKeywords);
            TxtDefaultProcs.Text    = string.Join(Environment.NewLine, AppSettings.DefaultProcesses);

            TxtExtraPorts.Text    = string.Join(Environment.NewLine, AppSettings.ExtraPorts);
            TxtExtraServices.Text = string.Join(Environment.NewLine, AppSettings.ExtraServices);
            TxtExtraTasks.Text    = string.Join(Environment.NewLine, AppSettings.ExtraTaskKeywords);
            TxtExtraProcs.Text    = string.Join(Environment.NewLine, AppSettings.ExtraProcesses);
            TxtExtraFiles.Text    = string.Join(Environment.NewLine, AppSettings.ExtraFilePaths);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        private static System.Collections.Generic.List<string> ParseLines(string text) =>
            text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

        // ── Buttons ───────────────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Parse ports (integers only)
            AppSettings.ExtraPorts = ParseLines(TxtExtraPorts.Text)
                .Select(l => int.TryParse(l, out int p) ? p : -1)
                .Where(p => p > 0 && p <= 65535)
                .Distinct()
                .ToList();

            AppSettings.ExtraServices     = ParseLines(TxtExtraServices.Text);
            AppSettings.ExtraTaskKeywords = ParseLines(TxtExtraTasks.Text);
            AppSettings.ExtraProcesses    = ParseLines(TxtExtraProcs.Text);
            AppSettings.ExtraFilePaths    = ParseLines(TxtExtraFiles.Text);

            AppSettings.Save();

            MessageBox.Show(
                L.Get("SD_Saved") + "\n" + AppSettings.SettingsFilePath,
                L.Get("SD_Title"),
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
