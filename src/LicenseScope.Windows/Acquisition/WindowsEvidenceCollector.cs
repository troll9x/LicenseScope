using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;
using LicenseScope.Windows.Models;

namespace LicenseScope.Windows.Acquisition
{
    public sealed class WindowsEvidenceCollector : IWindowsEvidenceCollector
    {
        internal const string WindowsApplicationId = "55c92734-d682-4d71-983e-d6ec3f16059f";
        private readonly IWmiQueryService _wmi;
        private readonly IWindowsRegistryReader _registry;
        private readonly WindowsSlmgrEvidenceProvider _slmgr;

        public WindowsEvidenceCollector(IWmiQueryService wmi, IWindowsRegistryReader registry, WindowsSlmgrEvidenceProvider slmgr)
        {
            _wmi = wmi ?? throw new ArgumentNullException(nameof(wmi));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _slmgr = slmgr ?? throw new ArgumentNullException(nameof(slmgr));
        }

        public async Task<WindowsLicenseEvidence> CollectAsync(SystemContext context, CancellationToken cancellationToken)
        {
            var products = new List<WindowsLicenseProductRecord>();
            var warnings = new List<string>();
            var oa3 = string.Empty;
            var backup = string.Empty;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rows = _wmi.Query("SELECT ID, ApplicationID, Name, Description, LicenseStatus, PartialProductKey, ProductKeyChannel, GracePeriodRemaining, EvaluationEndDate, KeyManagementServiceMachine, KeyManagementServicePort FROM SoftwareLicensingProduct WHERE ApplicationID='" + WindowsApplicationId + "'");
                foreach (var row in rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    products.Add(MapProduct(row));
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { warnings.Add("WMI product evidence unavailable (" + ex.GetType().Name + ")."); }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var serviceRows = _wmi.Query("SELECT OA3xOriginalProductKey FROM SoftwareLicensingService");
                if (serviceRows.Count > 0) oa3 = NormalizeProductKey(GetString(serviceRows[0], "OA3xOriginalProductKey"));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { warnings.Add("OEM firmware evidence unavailable (" + ex.GetType().Name + ")."); }

            var registryResult = _registry.ReadLocalMachineString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", "BackupProductKeyDefault", RegistryViewPreference.Registry64);
            if (registryResult.Found) backup = NormalizeProductKey(registryResult.Value);
            else if (registryResult.AccessDenied) warnings.Add("Registry evidence access denied.");
            else if (registryResult.ErrorMessage.Length > 0) warnings.Add("Registry evidence unavailable.");

            SlmgrEvidenceResult slmgr;
            try { slmgr = await _slmgr.CollectAsync(context.WindowsDirectory, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                warnings.Add("slmgr evidence unavailable (" + ex.GetType().Name + ").");
                slmgr = new SlmgrEvidenceResult();
            }
            if (slmgr.Warning.Length > 0) warnings.Add(slmgr.Warning);
            return new WindowsLicenseEvidence { Products = products, Xpr = slmgr.Xpr, Dlv = slmgr.Dlv, Oa3ProductKey = oa3, BackupProductKey = backup, Warnings = warnings };
        }

        private static WindowsLicenseProductRecord MapProduct(IReadOnlyDictionary<string, object?> row)
        {
            return new WindowsLicenseProductRecord
            {
                ApplicationId = GetString(row, "ApplicationID"),
                ActivationId = GetString(row, "ID"),
                Name = GetString(row, "Name"),
                Description = GetString(row, "Description"),
                LicenseStatus = GetUInt(row, "LicenseStatus"),
                PartialProductKey = LastFive(GetString(row, "PartialProductKey")),
                ProductKeyChannel = GetString(row, "ProductKeyChannel"),
                GracePeriodRemaining = GetUInt(row, "GracePeriodRemaining"),
                EvaluationEndDate = GetDate(row, "EvaluationEndDate"),
                KmsMachineName = GetString(row, "KeyManagementServiceMachine"),
                KmsPort = GetUInt(row, "KeyManagementServicePort")
            };
        }

        private static string GetString(IReadOnlyDictionary<string, object?> row, string key)
        {
            object? value;
            return row.TryGetValue(key, out value) && value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;
        }

        private static uint? GetUInt(IReadOnlyDictionary<string, object?> row, string key)
        {
            object? value;
            uint parsed;
            return row.TryGetValue(key, out value) && value != null && uint.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out parsed) ? parsed : (uint?)null;
        }

        private static DateTimeOffset? GetDate(IReadOnlyDictionary<string, object?> row, string key)
        {
            var text = GetString(row, key);
            DateTimeOffset parsed;
            return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed) ? parsed : (DateTimeOffset?)null;
        }

        private static string LastFive(string value)
        {
            var compact = (value ?? string.Empty).Replace("-", string.Empty).Trim();
            return compact.Length <= 5 ? compact.ToUpperInvariant() : compact.Substring(compact.Length - 5).ToUpperInvariant();
        }

        private static string NormalizeProductKey(string value)
        {
            var compact = (value ?? string.Empty).Replace("-", string.Empty).Trim();
            if (compact.Length != 25) return string.Empty;
            foreach (var character in compact)
            {
                if (!char.IsLetterOrDigit(character)) return string.Empty;
            }
            compact = compact.ToUpperInvariant();
            return compact.Substring(0, 5) + "-" + compact.Substring(5, 5) + "-" +
                   compact.Substring(10, 5) + "-" + compact.Substring(15, 5) + "-" +
                   compact.Substring(20, 5);
        }
    }
}
