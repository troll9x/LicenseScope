using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using WinLic.Scanners.Autodesk.Models;

namespace WinLic.Scanners.Autodesk.Parsing
{
    public sealed class AdskLicensingListParser
    {
        public AutodeskEvidenceResult Parse(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return Fail("Autodesk helper returned empty output.");
            var start = output.IndexOf('['); var end = output.LastIndexOf(']'); if (start < 0 || end < start) return Fail("Autodesk helper output format is unsupported.");
            try
            {
                var raw = new JavaScriptSerializer().DeserializeObject(output.Substring(start, end - start + 1)) as IEnumerable; if (raw == null) return Fail("Autodesk helper output did not contain an array.");
                var unique = new Dictionary<string, AutodeskRegistration>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in raw)
                {
                    var record = item as Dictionary<string, object>; if (record == null) continue;
                    var registration = Map(record); var identity = registration.SelectedProductKey + "|" + registration.SelectedProductVersion + "|" + registration.FeatureId;
                    if (!unique.ContainsKey(identity)) unique[identity] = registration;
                }
                return new AutodeskEvidenceResult { Successful = true, Registrations = unique.Values.OrderBy(x => x.SelectedProductKey).ThenBy(x => x.SelectedProductVersion).ToArray() };
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException) { return Fail("Autodesk helper output could not be parsed."); }
        }
        private static AutodeskRegistration Map(IDictionary<string, object> r) => new AutodeskRegistration {
            FeatureId = Text(r, "feature_id"), DefaultProductKey = Text(r, "def_prod_key"), DefaultProductVersion = Text(r, "def_prod_ver"), SelectedProductKey = Text(r, "sel_prod_key"), SelectedProductVersion = Text(r, "sel_prod_ver"),
            DefaultProductCode = Text(r, "def_prod_code"), SelectedProductCode = Text(r, "sel_prod_code"), LicenseMethodCode = Number(r, "lic_method"), SupportedLicenseMethodCodes = Numbers(r, "supported_lic_methods"), LicenseServerTypeCode = Number(r, "lic_server_type"), HasConfiguredServers = Count(r, "lic_servers") > 0 };
        private static string Text(IDictionary<string, object> r, string key) => r.TryGetValue(key, out var value) && value != null ? Convert.ToString(value) ?? "" : "";
        private static int? Number(IDictionary<string, object> r, string key) { if (!r.TryGetValue(key, out var value) || value == null) return null; return int.TryParse(Convert.ToString(value), out var number) ? number : (int?)null; }
        private static IReadOnlyList<int> Numbers(IDictionary<string, object> r, string key) { if (!r.TryGetValue(key, out var value) || !(value is IEnumerable values)) return Array.Empty<int>(); var result = new List<int>(); foreach (var item in values) if (int.TryParse(Convert.ToString(item), out var number)) result.Add(number); return result; }
        private static int Count(IDictionary<string, object> r, string key) { if (!r.TryGetValue(key, out var value) || !(value is IEnumerable values)) return 0; var count = 0; foreach (var ignored in values) count++; return count; }
        private static AutodeskEvidenceResult Fail(string warning) => new AutodeskEvidenceResult { Warnings = new[] { warning } };
    }
}
