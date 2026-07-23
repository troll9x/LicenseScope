using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using LicenseScope.Core.Security;
using LicenseScope.Office.Models;

namespace LicenseScope.Office.Parsing
{
    public interface IOsppStatusParser { IReadOnlyList<OfficeProductEvidence> Parse(string output, CultureInfo culture); }
    public interface IVNextStatusParser { IReadOnlyList<OfficeProductEvidence> Parse(string output, CultureInfo culture); }

    internal static class OfficeParseHelpers
    {
        public static string Value(string line) { var i = line.IndexOf(':'); return i < 0 ? string.Empty : line.Substring(i + 1).Trim(); }
        public static string LastFive(string value) { var compact = Regex.Replace(value ?? string.Empty, "[^A-Za-z0-9]", string.Empty); return compact.Length < 5 ? compact.ToUpperInvariant() : compact.Substring(compact.Length - 5).ToUpperInvariant(); }
        public static OfficeProductFamily Family(string text) { var v = (text ?? string.Empty).ToLowerInvariant(); return v.Contains("project") ? OfficeProductFamily.Project : v.Contains("visio") ? OfficeProductFamily.Visio : v.Contains("access") ? OfficeProductFamily.Access : v.Contains("o365") || v.Contains("microsoft 365") ? OfficeProductFamily.Microsoft365Apps : OfficeProductFamily.OfficeSuite; }
        public static string Name(string raw, OfficeProductFamily family) { if (!string.IsNullOrWhiteSpace(raw)) return raw.Trim(); return family == OfficeProductFamily.Project ? "Microsoft Project" : family == OfficeProductFamily.Visio ? "Microsoft Visio" : family == OfficeProductFamily.Microsoft365Apps ? "Microsoft 365 Apps" : "Microsoft Office product"; }
        public static DateTimeOffset? Date(string value, CultureInfo culture) { DateTimeOffset result; return DateTimeOffset.TryParse(value, culture, DateTimeStyles.AssumeLocal, out result) || DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result) ? result : (DateTimeOffset?)null; }
    }

    public sealed class OsppStatusParser : IOsppStatusParser
    {
        public IReadOnlyList<OfficeProductEvidence> Parse(string output, CultureInfo culture)
        {
            var results = new List<OfficeProductEvidence>(); OfficeProductEvidence? current = null;
            foreach (var raw in (output ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var line = raw.Trim(); if (line.Length == 0) continue;
                if (line.IndexOf("LICENSE NAME", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("TÊN GIẤY PHÉP", StringComparison.OrdinalIgnoreCase) >= 0)
                { if (current != null && HasData(current)) results.Add(current); current = new OfficeProductEvidence { FromOfficialTool = true }; var v = OfficeParseHelpers.Value(line); current.ProductId = v; current.Family = OfficeParseHelpers.Family(v); current.ProductName = OfficeParseHelpers.Name(v, current.Family); }
                else if (current == null && line.IndexOf("PRODUCT ID", StringComparison.OrdinalIgnoreCase) >= 0)
                { var v = OfficeParseHelpers.Value(line); current = new OfficeProductEvidence { ProductId = v, ProductName = "Microsoft Office product", Family = OfficeProductFamily.Unknown, FromOfficialTool = true }; }
                if (current == null) continue;
                if (line.IndexOf("LICENSE DESCRIPTION", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("MÔ TẢ GIẤY PHÉP", StringComparison.OrdinalIgnoreCase) >= 0) current.Channel = OfficeParseHelpers.Value(line);
                else if (line.IndexOf("LICENSE STATUS", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("TRẠNG THÁI GIẤY PHÉP", StringComparison.OrdinalIgnoreCase) >= 0) current.LicenseState = OfficeParseHelpers.Value(line);
                else if (line.IndexOf("Last 5 characters", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("5 ký tự cuối", StringComparison.OrdinalIgnoreCase) >= 0) current.PartialProductKey = OfficeParseHelpers.LastFive(OfficeParseHelpers.Value(line));
                else if (line.IndexOf("REMAINING GRACE", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("THỜI GIAN GIA HẠN", StringComparison.OrdinalIgnoreCase) >= 0) current.ExpirationKind = "GraceOrKmsActivation";
            }
            if (current != null && HasData(current)) results.Add(current);
            return results;
        }
        private static bool HasData(OfficeProductEvidence p) { return p.ProductId.Length > 0 || p.LicenseState.Length > 0 || p.Channel.Length > 0; }
    }

    public sealed class VNextStatusParser : IVNextStatusParser
    {
        public IReadOnlyList<OfficeProductEvidence> Parse(string output, CultureInfo culture)
        {
            var results = new List<OfficeProductEvidence>(); OfficeProductEvidence? current = null;
            foreach (var raw in (output ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var line = raw.Trim(); if (line.Length == 0) continue;
                if (Starts(line, "Name", "Product", "Tên sản phẩm")) { if (current != null) results.Add(current); var value = OfficeParseHelpers.Value(line); var family = OfficeParseHelpers.Family(value); current = new OfficeProductEvidence { ProductId = value, ProductName = OfficeParseHelpers.Name(value, family), Family = family, FromOfficialTool = true }; continue; }
                if (current == null) continue;
                if (Starts(line, "License Type", "Type", "Loại giấy phép")) current.Channel = OfficeParseHelpers.Value(line);
                else if (Starts(line, "State", "License State", "Trạng thái")) current.LicenseState = OfficeParseHelpers.Value(line);
                else if (Starts(line, "Email", "User", "Người dùng")) current.MaskedAccount = SensitiveDataMasker.MaskEmail(OfficeParseHelpers.Value(line));
                else if (Starts(line, "Mode", "Activation Mode", "Chế độ")) current.LicenseMode = OfficeParseHelpers.Value(line);
                else if (Starts(line, "Expiration", "Expiry", "Hết hạn")) { current.ExpirationDate = OfficeParseHelpers.Date(OfficeParseHelpers.Value(line), culture); current.ExpirationKind = "CachedEntitlementExpiration"; }
            }
            if (current != null) results.Add(current);
            return results;
        }
        private static bool Starts(string line, params string[] labels) { foreach (var label in labels) if (line.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase)) return true; return false; }
    }
}
