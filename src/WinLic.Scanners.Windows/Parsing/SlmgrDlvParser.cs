using System;
using System.Globalization;
using WinLic.Scanners.Windows.Models;

namespace WinLic.Scanners.Windows.Parsing
{
    public sealed class SlmgrDlvParser : ISlmgrDlvParser
    {
        public SlmgrDlvParseResult Parse(string output, CultureInfo culture)
        {
            var text = output ?? string.Empty;
            var result = new SlmgrDlvParseResult();
            foreach (var raw in SlmgrXprParser.SplitLines(text))
            {
                var line = raw.Trim();
                Assign(line, new[] { "Description", "Mô tả" }, value => result.Description = value);
                Assign(line, new[] { "License Status", "Trạng thái giấy phép", "Trạng thái bản quyền" }, value => result.LicenseStatusText = value);
                Assign(line, new[] { "Partial Product Key", "Khóa sản phẩm một phần", "Mã khóa sản phẩm một phần" }, value => result.PartialProductKey = LastFive(value));
                Assign(line, new[] { "KMS machine name", "Tên máy KMS" }, value => result.KmsMachineName = value);
            }
            result.ExpirationDate = SlmgrXprParser.TryParseDate(text, culture);
            result.Parsed = result.Description.Length > 0 || result.LicenseStatusText.Length > 0 || result.PartialProductKey.Length > 0 || result.KmsMachineName.Length > 0 || result.ExpirationDate.HasValue;
            return result;
        }

        private static void Assign(string line, string[] labels, Action<string> setter)
        {
            foreach (var label in labels)
            {
                if (!line.StartsWith(label, StringComparison.OrdinalIgnoreCase)) continue;
                var colon = line.IndexOf(':');
                if (colon >= 0 && colon < line.Length - 1) setter(line.Substring(colon + 1).Trim());
                return;
            }
        }

        private static string LastFive(string value)
        {
            var compact = (value ?? string.Empty).Replace("-", string.Empty).Trim();
            return compact.Length <= 5 ? compact.ToUpperInvariant() : compact.Substring(compact.Length - 5).ToUpperInvariant();
        }
    }
}
