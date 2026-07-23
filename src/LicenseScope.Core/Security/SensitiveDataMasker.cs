using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LicenseScope.Core.Security
{
    /// <summary>Masks common sensitive values before they enter logs or reports.</summary>
    public static class SensitiveDataMasker
    {
        private static readonly Regex ProductKey = new Regex("^[A-Za-z0-9]{5}(?:-[A-Za-z0-9]{5}){4}$", RegexOptions.CultureInvariant);
        private static readonly Regex ProductKeyAnywhere = new Regex(@"\b[A-Za-z0-9]{5}(?:-[A-Za-z0-9]{5}){4}\b", RegexOptions.CultureInvariant);
        private static readonly Regex UserPath = new Regex(@"(?i)([A-Z]:\\Users\\)[^\\]+", RegexOptions.CultureInvariant);
        private static readonly Regex EmailAnywhere = new Regex(@"(?i)\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b", RegexOptions.CultureInvariant);
        private static readonly Regex NamedSecret = new Regex(
            @"(?i)\b(token|password|secret|credential|cookie|authorization)\b\s*[:=]\s*[^\s;,]+",
            RegexOptions.CultureInvariant);
        private static readonly string[] SecretNames = { "token", "password", "secret", "credential", "cookie", "session", "authorization" };

        public static string MaskProductKey(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var nonNullValue = value ?? string.Empty;
            if (!ProductKey.IsMatch(nonNullValue)) return nonNullValue;
            return "XXXXX-XXXXX-XXXXX-XXXXX-" + nonNullValue.Substring(nonNullValue.Length - 5).ToUpperInvariant();
        }

        public static string MaskEmail(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var nonNullValue = value ?? string.Empty;
            var at = nonNullValue.IndexOf('@');
            if (at <= 0 || at != nonNullValue.LastIndexOf('@') || at == nonNullValue.Length - 1) return nonNullValue;
            var local = nonNullValue.Substring(0, at);
            var masked = local.Length == 1 ? "*" : local[0] + new string('*', Math.Max(1, local.Length - 2)) + local[local.Length - 1];
            return masked + nonNullValue.Substring(at);
        }

        public static string AnonymizeMachineName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var nonNullValue = value ?? string.Empty;
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(nonNullValue.Trim().ToUpperInvariant()));
                return "MACHINE-" + string.Concat(hash.Take(6).Select(b => b.ToString("X2")));
            }
        }

        public static string MaskWindowsPath(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return UserPath.Replace(value, "$1<USER>");
        }

        public static bool IsSensitiveFieldName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var nonNullName = name ?? string.Empty;
            return SecretNames.Any(secret => nonNullName.IndexOf(secret, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static string MaskNamedValue(string? name, string? value)
        {
            return IsSensitiveFieldName(name) ? "[REDACTED]" : value ?? string.Empty;
        }

        public static string SanitizeDiagnosticText(string? value)
        {
            var sanitized = MaskWindowsPath(value ?? string.Empty);
            sanitized = ProductKeyAnywhere.Replace(
                sanitized,
                match => "XXXXX-XXXXX-XXXXX-XXXXX-" +
                         match.Value.Substring(match.Value.Length - 5).ToUpperInvariant());
            sanitized = EmailAnywhere.Replace(sanitized, match => MaskEmail(match.Value));
            return NamedSecret.Replace(sanitized, match => match.Groups[1].Value + "=[REDACTED]");
        }
    }
}
