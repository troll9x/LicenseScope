using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using LicenseScope.Core.Models;
using LicenseScope.Core.Runtime;
using LicenseScope.Reporting;

namespace LicenseScope.App
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource? _cancellation;
        private AuditResult? _result;
        private IReadOnlyList<LicenseResult> _displayProducts = Array.Empty<LicenseResult>();
        private IReadOnlyList<string> _maskedProductKeys = Array.Empty<string>();
        private IReadOnlyList<string> _fullProductKeys = Array.Empty<string>();
        private readonly KmsManagementService _kmsManagement = new KmsManagementService();
        private readonly SoftwareUninstallService _softwareUninstall = new SoftwareUninstallService();
        private bool _revealConfirmed;

        public MainWindow()
        {
            InitializeComponent();
            try { ApplicationDataPaths.EnsureSettingsDirectory(); }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.IO.IOException)
            {
                StatusText.Text = "Không thể mở thư mục cài đặt; vẫn có thể quét ở chế độ chỉ đọc.";
            }
        }

        private async void Scan_Click(object sender, RoutedEventArgs e)
        {
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            SetRunning(true);
            ScanProgress.Value = 0;
            StatusText.Text = "Đang bắt đầu kiểm tra chỉ đọc…";
            var progress = new Progress<AuditProgress>(p =>
            {
                StatusText.Text = $"Đang quét {p.ScannerId} ({p.CurrentIndex}/{p.TotalScannerCount})";
                ScanProgress.Value = p.TotalScannerCount == 0 ? 0 : (p.CurrentIndex - 1) * 100d / p.TotalScannerCount;
            });

            try
            {
                var result = await ApplicationCompositionRoot.CreateUnifiedAudit(AuditSettings.Load())
                    .RunAllAsync(_cancellation.Token, progress);
                DisplayAuditResult(result);
                ScanProgress.Value = 100;
                var summary = AuditSummary.From(result.Products);
                StatusText.Text = result.WasCancelled
                    ? "Đã hủy kiểm tra."
                    : $"Hoàn tất: {summary.Total} sản phẩm; {summary.Licensed} đã cấp phép; {summary.Attention + summary.Unknown} cần xem xét.";
            }
            catch (OperationCanceledException)
            {
                StatusText.Text = "Đã hủy kiểm tra.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Kiểm tra dừng an toàn do lỗi " + ex.GetType().Name + ".";
            }
            finally
            {
                SetRunning(false);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => _cancellation?.Cancel();

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilter();
        }

        private void RevealKeys_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (RevealKeysCheckBox.IsChecked == true && !_revealConfirmed)
            {
                var answer = MessageBox.Show(
                    this,
                    "Khóa sản phẩm đầy đủ là dữ liệu nhạy cảm. Chỉ hiển thị khi không có người khác quan sát hoặc ghi lại màn hình.\n\nBáo cáo xuất ra vẫn luôn che khóa.",
                    "Hiển thị khóa sản phẩm đầy đủ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (answer != MessageBoxResult.Yes)
                {
                    RevealKeysCheckBox.IsChecked = false;
                    return;
                }
                _revealConfirmed = true;
            }
            RefreshDisplayedKeys();
            ApplyFilter();
        }

        private void RefreshDisplayedKeys()
        {
            for (var index = 0; index < _displayProducts.Count; index++)
            {
                var masked = index < _maskedProductKeys.Count ? _maskedProductKeys[index] : string.Empty;
                var full = index < _fullProductKeys.Count ? _fullProductKeys[index] : string.Empty;
                _displayProducts[index].PartialProductKey =
                    RevealKeysCheckBox.IsChecked == true && !string.IsNullOrWhiteSpace(full) ? full : masked;
            }
        }

        private void DisplayAuditResult(AuditResult result)
        {
            _result = result;
            _displayProducts = new AuditResultSanitizer()
                .CreateReportSnapshot(result, new ReportWriteOptions()).Products.ToArray();
            _maskedProductKeys = _displayProducts.Select(x => x.PartialProductKey).ToArray();
            _fullProductKeys = result.Products.Select(x => x.FullProductKey).ToArray();
            RefreshDisplayedKeys();
            ApplyFilter();
        }

        private async Task<AuditResult?> RunWindowsFocusedAuditAsync(string startingText)
        {
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            SetRunning(true);
            ScanProgress.Value = 0;
            StatusText.Text = startingText;
            try
            {
                var services = ApplicationCompositionRoot.CreateWindowsAudit(AuditSettings.Load());
                var progress = new Progress<AuditProgress>(p =>
                {
                    ScanProgress.Value = p.TotalScannerCount == 0
                        ? 0
                        : p.CurrentIndex * 100d / p.TotalScannerCount;
                });
                var result = await services.Orchestrator.RunAllAsync(
                    services.Context,
                    _cancellation.Token,
                    progress);
                DisplayAuditResult(result);
                ScanProgress.Value = 100;
                ResultsGrid.SelectedIndex = _displayProducts.Count > 0 ? 0 : -1;
                if (ResultsGrid.SelectedItem != null) ResultsGrid.ScrollIntoView(ResultsGrid.SelectedItem);
                return result;
            }
            catch (OperationCanceledException)
            {
                StatusText.Text = "Đã hủy kiểm tra.";
                return null;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Kiểm tra dừng an toàn do lỗi " + ex.GetType().Name + ".";
                return null;
            }
            finally
            {
                SetRunning(false);
            }
        }

        private async void CheckKms_Click(object sender, RoutedEventArgs e)
        {
            var snapshot = await Task.Run(() => _kmsManagement.Probe());
            var result = await RunWindowsFocusedAuditAsync("Đang kiểm tra cấu hình KMS Windows…");
            if (result == null) return;
            StatusText.Text = snapshot.IsConfigured
                ? "Đã phát hiện cấu hình KMS. Chọn hàng Windows để xem chi tiết."
                : "Không phát hiện máy chủ KMS được cấu hình thủ công.";
            MessageBox.Show(this, snapshot.Describe(), "Kiểm tra KMS",
                MessageBoxButton.OK,
                snapshot.IsConfigured ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }

        private async void CheckThirdParty_Click(object sender, RoutedEventArgs e)
        {
            var result = await RunWindowsFocusedAuditAsync("Đang kiểm tra chỉ dấu kích hoạt bên thứ ba…");
            if (result == null) return;
            var warnings = result.Products.SelectMany(x => x.Warnings)
                .Count(x => x.StartsWith("Cài đặt kiểm tra:", StringComparison.OrdinalIgnoreCase));
            StatusText.Text = warnings == 0
                ? "Không phát hiện chỉ dấu kích hoạt bên thứ ba theo cấu hình hiện tại."
                : "Phát hiện " + warnings + " chỉ dấu cần xem xét. Chọn hàng Windows để xem chi tiết.";
        }

        private async void ClearKms_Click(object sender, RoutedEventArgs e)
        {
            var before = await Task.Run(() => _kmsManagement.Probe());
            if (!before.IsConfigured)
            {
                MessageBox.Show(this, before.Describe(), "Xóa cấu hình KMS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmation = new KmsClearConfirmationDialog(before.Describe()) { Owner = this };
            if (confirmation.ShowDialog() != true) return;

            SetRunning(true);
            StatusText.Text = "Đang yêu cầu quyền quản trị để xóa cấu hình KMS Windows…";
            KmsClearResult clearResult;
            try
            {
                clearResult = await _kmsManagement.ClearWindowsKmsSettingsAsync();
            }
            finally
            {
                SetRunning(false);
            }

            if (!clearResult.Success)
            {
                StatusText.Text = clearResult.ErrorMessage;
                MessageBox.Show(this, clearResult.ErrorMessage, "Xóa cấu hình KMS",
                    MessageBoxButton.OK,
                    clearResult.WasCancelled ? MessageBoxImage.Information : MessageBoxImage.Error);
                return;
            }

            var after = await Task.Run(() => _kmsManagement.Probe());
            var verified = !after.IsConfigured;
            StatusText.Text = verified
                ? "Đã xóa và xác minh cấu hình KMS Windows."
                : "Windows đã chạy lệnh xóa nhưng vẫn còn cấu hình KMS; cần kiểm tra thủ công.";
            MessageBox.Show(this, StatusText.Text, "Xóa cấu hình KMS",
                MessageBoxButton.OK, verified ? MessageBoxImage.Information : MessageBoxImage.Warning);
            await RunWindowsFocusedAuditAsync("Đang kiểm tra lại sau khi xóa cấu hình KMS…");
            StatusText.Text = verified
                ? "Đã xóa và xác minh cấu hình KMS Windows."
                : "Vẫn còn cấu hình KMS; cần kiểm tra thủ công.";
        }

        private async void UninstallSoftware_Click(object sender, RoutedEventArgs e)
        {
            var product = (sender as FrameworkElement)?.DataContext as LicenseResult;
            if (product == null || !product.Installed || product.IsLicensed == true ||
                product.ScannerId.Equals("microsoft.windows", StringComparison.OrdinalIgnoreCase))
                return;

            StatusText.Text = "Đang đối chiếu với danh sách ứng dụng đã cài đặt…";
            var registered = await Task.Run(() => _softwareUninstall.FindRegisteredProgram(product));
            if (registered == null)
            {
                var openSettings = MessageBox.Show(
                    this,
                    "Không tìm thấy trình gỡ cài đặt khớp đủ chắc chắn với “" +
                    product.ProductName +
                    "”.\n\nMở trang Ứng dụng đã cài đặt của Windows để bạn chọn thủ công?",
                    "Không thể gỡ tự động",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (openSettings == MessageBoxResult.Yes)
                    StatusText.Text = _softwareUninstall.OpenInstalledApps()
                        ? "Đã mở trang Ứng dụng đã cài đặt của Windows."
                        : "Windows không thể mở trang Ứng dụng đã cài đặt.";
                else
                    StatusText.Text = "Đã hủy gỡ phần mềm.";
                return;
            }

            var confirmation = new SoftwareUninstallConfirmationDialog(
                product.ProductName,
                registered.DisplayName +
                (string.IsNullOrWhiteSpace(registered.DisplayVersion)
                    ? string.Empty
                    : " " + registered.DisplayVersion))
            {
                Owner = this
            };
            if (confirmation.ShowDialog() != true)
            {
                StatusText.Text = "Đã hủy gỡ phần mềm.";
                return;
            }

            SetRunning(true);
            StatusText.Text = "Đang yêu cầu Windows mở trình gỡ cài đặt…";
            SoftwareUninstallResult uninstallResult;
            try
            {
                uninstallResult = await _softwareUninstall.StartUninstallAsync(registered);
            }
            finally
            {
                SetRunning(false);
            }

            if (uninstallResult.Started)
            {
                StatusText.Text = "Đã mở trình gỡ cho " + registered.DisplayName +
                                  ". Hãy hoàn tất trong cửa sổ của nhà cung cấp rồi quét lại.";
                return;
            }

            var message = uninstallResult.ErrorMessage;
            if (uninstallResult.RequiresManualRemoval)
            {
                var openSettings = MessageBox.Show(
                    this,
                    message + "\n\nMở trang Ứng dụng đã cài đặt của Windows?",
                    "Cần gỡ thủ công",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (openSettings == MessageBoxResult.Yes)
                    _softwareUninstall.OpenInstalledApps();
            }
            else
            {
                MessageBox.Show(
                    this,
                    message,
                    "Không thể mở trình gỡ",
                    MessageBoxButton.OK,
                    uninstallResult.WasCancelled
                        ? MessageBoxImage.Information
                        : MessageBoxImage.Error);
            }
            StatusText.Text = message;
        }

        private async void CrackTrace_Click(object sender, RoutedEventArgs e)
        {
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            SetRunning(true);
            var options = new CrackTraceScanOptions();
            if (DeepForensicCheckBox.IsChecked == true)
            {
                var consent = MessageBox.Show(
                    this,
                    "Quét pháp chứng chuyên sâu chỉ đọc nhật ký sự kiện cấp phép Windows, nhật ký vận hành PowerShell khi hệ thống có ghi nhật ký, lịch sử phát hiện của Defender và các mục Prefetch/Amcache nằm trong danh sách cho phép.\n\nKhông quét tệp người dùng, không tải dữ liệu lên mạng và không sửa hoặc xóa dữ liệu. Bạn có đồng ý chạy kiểm tra sâu này?",
                    "Đồng ý quét pháp chứng chuyên sâu",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (consent != MessageBoxResult.Yes)
                {
                    DeepForensicCheckBox.IsChecked = false;
                    SetRunning(false);
                    StatusText.Text = "Đã hủy quét pháp chứng chuyên sâu vì chưa có sự đồng ý.";
                    return;
                }
                options.DeepForensicScan = true;
                options.UserConsented = true;
            }
            StatusText.Text = options.DeepForensicScan
                ? "Đang phân tích dấu vết bằng chế độ pháp chứng chuyên sâu chỉ đọc…"
                : "Đang phân tích 7 nhóm dấu vết hiện tại ở chế độ chỉ đọc…";
            try
            {
                var context = new DefaultSystemContextProvider().GetCurrent();
                var analysis = await ApplicationCompositionRoot.CreateCrackTraceAnalyzer()
                    .AnalyzeAsync(context, options, _cancellation.Token);
                if (_result == null)
                {
                    var now = DateTimeOffset.Now;
                    _result = new AuditResult
                    {
                        System = context,
                        StartedAt = now,
                        CompletedAt = now,
                        Products = Array.Empty<LicenseResult>(),
                        ScannerExecutions = Array.Empty<ScannerExecutionResult>()
                    };
                }
                _result.CrackTraceAnalysis = analysis;
                RenderCrackTrace(analysis);
                CrackTracePanel.Visibility = Visibility.Visible;
                CrackTracePanel.IsExpanded = true;
                StatusText.Text = "Phân tích hoàn tất: Phát hiện dấu vết=" +
                                  (analysis.TraceDetected ? "CÓ" : "KHÔNG") +
                                  "; Quét hoàn tất=" +
                                  (analysis.ScanCompleted ? "CÓ" : "KHÔNG") + ".";
            }
            catch (OperationCanceledException)
            {
                StatusText.Text = "Đã hủy phân tích dấu vết.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Phân tích dừng an toàn do lỗi " + ex.GetType().Name + ".";
            }
            finally
            {
                SetRunning(false);
            }
        }

        private void RenderCrackTrace(CrackTraceAnalysisResult analysis)
        {
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                PagePadding = new Thickness(0)
            };
            foreach (var line in new CrackTraceTextFormatter().Format(analysis))
            {
                var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
                paragraph.Inlines.Add(new Run(line.Text)
                {
                    Foreground = BrushFor(line),
                    FontWeight = line.IsHeading || line.IsVerdict
                        ? FontWeights.Bold
                        : FontWeights.Normal
                });
                document.Blocks.Add(paragraph);
            }
            CrackTraceLog.Document = document;
        }

        private static Brush BrushFor(CrackTraceDisplayLine line)
        {
            if (line.Status == CrackTraceStatus.TraceNotFound) return Brushes.LightSkyBlue;
            if (line.Status == CrackTraceStatus.Suspicious) return Brushes.Gold;
            if (line.Status == CrackTraceStatus.Detected) return Brushes.OrangeRed;
            if (line.Status == CrackTraceStatus.Unknown) return Brushes.LightGray;
            if (line.Status == CrackTraceStatus.Error) return Brushes.LightCoral;
            return line.IsHeading ? Brushes.LightSkyBlue : Brushes.Gainsboro;
        }

        private void ApplyFilter()
        {
            IEnumerable<LicenseResult> values = _displayProducts;
            var filter = (StatusFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
            if (filter == "Licensed") values = values.Where(x => x.Status == LicenseStatus.Licensed);
            else if (filter == "Unlicensed") values = values.Where(x => x.Status == LicenseStatus.Unlicensed || x.Status == LicenseStatus.Expired);
            else if (filter == "Attention") values = values.Where(x => x.Status == LicenseStatus.Trial || x.Status == LicenseStatus.GracePeriod || x.Status == LicenseStatus.NeedsSignIn || x.Status == LicenseStatus.NeedsOnlineVerification);
            else if (filter == "Unknown") values = values.Where(x => x.Status == LicenseStatus.Unknown || x.Status == LicenseStatus.Error || x.Status == LicenseStatus.Unsupported);
            ResultsGrid.ItemsSource = values.ToArray();
        }

        private async Task ExportAsync(string format)
        {
            if (_result == null) return;
            var dialog = new SaveFileDialog
            {
                Filter = format.ToUpperInvariant() + " files|*." + format,
                FileName = "LicenseScope-Audit-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "." + format
            };
            if (dialog.ShowDialog(this) != true) return;
            var sanitizer = new AuditResultSanitizer();
            IAuditReportWriter writer = format == "json"
                ? (IAuditReportWriter)new JsonAuditReportWriter(sanitizer)
                : format == "csv" ? new CsvAuditReportWriter(sanitizer) : new HtmlAuditReportWriter(sanitizer);
            var written = await writer.WriteAsync(_result, new ReportWriteOptions
            {
                OutputPath = dialog.FileName,
                IncludeEvidence = true,
                IncludeWarnings = true,
                Overwrite = true
            }, CancellationToken.None);
            StatusText.Text = written.Success ? "Đã lưu báo cáo: " + written.OutputPath : "Không thể lưu báo cáo: " + written.ErrorMessage;
        }

        private async void ExportJson_Click(object sender, RoutedEventArgs e) => await ExportAsync("json");
        private async void ExportCsv_Click(object sender, RoutedEventArgs e) => await ExportAsync("csv");
        private async void ExportHtml_Click(object sender, RoutedEventArgs e) => await ExportAsync("html");
        private void AuditSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AuditSettingsDialog { Owner = this };
            if (dialog.ShowDialog() == true)
                StatusText.Text = "Đã lưu cài đặt kiểm tra. Cấu hình mới sẽ áp dụng ở lần quét tiếp theo.";
        }
        private void About_Click(object sender, RoutedEventArgs e) => new AboutDialog { Owner = this }.ShowDialog();

        private void SetRunning(bool running)
        {
            ScanButton.IsEnabled = !running;
            CancelButton.IsEnabled = running;
            var canExport = !running && _result != null;
            ExportJsonButton.IsEnabled = canExport;
            ExportCsvButton.IsEnabled = canExport;
            ExportHtmlButton.IsEnabled = canExport;
            CheckKmsButton.IsEnabled = !running;
            ClearKmsButton.IsEnabled = !running;
            ThirdPartyCheckButton.IsEnabled = !running;
            CrackTraceButton.IsEnabled = !running;
            DeepForensicCheckBox.IsEnabled = !running;
            ResultsGrid.IsEnabled = !running;
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            base.OnClosed(e);
        }
    }
}
