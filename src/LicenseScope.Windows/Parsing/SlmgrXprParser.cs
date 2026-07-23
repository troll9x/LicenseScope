using System;
using System.Globalization;
using System.Linq;
using LicenseScope.Windows.Models;

namespace LicenseScope.Windows.Parsing
{
    public sealed class SlmgrXprParser : ISlmgrXprParser
    {
        private static readonly string[] PermanentMarkers = { "permanently activated", "được kích hoạt vĩnh viễn", "kích hoạt vĩnh viễn" };
        private static readonly string[] UnlicensedMarkers = { "unlicensed", "notification mode", "chưa được cấp phép", "không được cấp phép", "chế độ thông báo" };

        public SlmgrXprParseResult Parse(string output, CultureInfo culture)
        {
            var text = output ?? string.Empty;
            var lower = text.ToLowerInvariant();
            var result = new SlmgrXprParseResult
            {
                RawSummary = FirstNonEmptyLine(text),
                IsPermanent = PermanentMarkers.Any(lower.Contains),
                IndicatesUnlicensed = UnlicensedMarkers.Any(lower.Contains)
            };
            result.ExpirationDate = TryParseDate(text, culture);
            result.Parsed = result.IsPermanent || result.IndicatesUnlicensed || result.ExpirationDate.HasValue;
            return result;
        }

        internal static DateTimeOffset? TryParseDate(string text, CultureInfo culture)
        {
            foreach (var line in SplitLines(text))
            {
                var colon = line.IndexOf(':');
                if (colon < 0 || colon == line.Length - 1) continue;
                var candidate = line.Substring(colon + 1).Trim();
                DateTimeOffset value;
                if (DateTimeOffset.TryParse(candidate, culture, DateTimeStyles.AssumeLocal, out value) ||
                    DateTimeOffset.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out value))
                    return value;
            }
            return null;
        }

        internal static string[] SplitLines(string text) => (text ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        private static string FirstNonEmptyLine(string text) => SplitLines(text).Select(line => line.Trim()).FirstOrDefault(line => line.Length > 0) ?? string.Empty;
    }
}
