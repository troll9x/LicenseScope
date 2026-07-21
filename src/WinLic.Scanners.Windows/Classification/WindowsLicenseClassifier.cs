using System;
using System.Collections.Generic;
using WinLic.Core.Contracts;
using WinLic.Core.Models;
using WinLic.Scanners.Windows.Models;

namespace WinLic.Scanners.Windows.Classification
{
    public sealed class WindowsLicenseClassifier
    {
        private readonly WindowsChannelClassifier _channels;
        private readonly IWindowsKnownKeyCatalog _keys;
        private readonly ISystemClock _clock;

        public WindowsLicenseClassifier(WindowsChannelClassifier channels, IWindowsKnownKeyCatalog keys, ISystemClock clock)
        {
            _channels = channels;
            _keys = keys;
            _clock = clock;
        }

        public WindowsActivationClassification Classify(WindowsLicenseProductRecord? product, WindowsLicenseEvidence evidence, bool ambiguous)
        {
            var warnings = new List<string>(evidence.Warnings ?? Array.Empty<string>());
            if (ambiguous) warnings.Add("Multiple equally ranked Windows license records were found.");
            if (product == null)
                return new WindowsActivationClassification { Status = evidence.Xpr.IndicatesUnlicensed ? LicenseStatus.Unlicensed : LicenseStatus.Unknown, Confidence = evidence.Xpr.Parsed ? ConfidenceLevel.Low : ConfidenceLevel.None, Warnings = warnings };

            var status = MapStatus(product.LicenseStatus, evidence.Xpr, warnings);
            var type = _channels.Classify(product.Description, product.ProductKeyChannel);
            var expiration = product.EvaluationEndDate ?? evidence.Xpr.ExpirationDate ?? evidence.Dlv.ExpirationDate;
            if (expiration.HasValue && expiration.Value < _clock.UtcNow && (type == "Evaluation" || type == "Volume_KMSCLIENT")) status = LicenseStatus.Expired;

            var method = ActivationMethod(type, product, evidence, warnings);
            var confidence = product.LicenseStatus.HasValue ? (evidence.Xpr.Parsed ? ConfidenceLevel.High : ConfidenceLevel.Medium) : evidence.Xpr.Parsed ? ConfidenceLevel.Low : ConfidenceLevel.None;
            if (ambiguous && confidence == ConfidenceLevel.High) confidence = ConfidenceLevel.Medium;
            if (type == "Volume_KMSCLIENT") warnings.Add("KMS client activation requires periodic renewal; KMS is a legitimate volume licensing mechanism.");
            if (evidence.MaskedOa3Key.Length > 0 && status != LicenseStatus.Licensed) warnings.Add("An OEM firmware key is present but does not prove the installed edition is activated.");
            return new WindowsActivationClassification { Status = status, LicenseType = type, ActivationMethod = method, Confidence = confidence, ExpirationDate = expiration, Warnings = warnings };
        }

        public static LicenseStatus MapStatus(uint? status, SlmgrXprParseResult xpr, IList<string> warnings)
        {
            switch (status)
            {
                case 0: return LicenseStatus.Unlicensed;
                case 1: return LicenseStatus.Licensed;
                case 2:
                case 3:
                case 6: return LicenseStatus.GracePeriod;
                case 4: warnings.Add("Windows reports a non-genuine grace validation state; no piracy conclusion was made."); return LicenseStatus.GracePeriod;
                case 5:
                    warnings.Add("Windows reports notification mode.");
                    return xpr.IndicatesUnlicensed ? LicenseStatus.Unlicensed : LicenseStatus.Unknown;
                default: return LicenseStatus.Unknown;
            }
        }

        private string ActivationMethod(string type, WindowsLicenseProductRecord product, WindowsLicenseEvidence evidence, IList<string> warnings)
        {
            if (type == "Volume_KMSCLIENT") return "KMS client";
            if (type == "Volume_MAK") return "MAK";
            if (type.StartsWith("OEM_", StringComparison.Ordinal)) return evidence.MaskedOa3Key.Length > 0 ? "OEM firmware key" : "OEM";
            if (type == "Retail" && product.LicenseStatus == 1 && evidence.Xpr.IsPermanent && _keys.IsGenericInstallationKey(product.PartialProductKey))
            {
                warnings.Add("Digital license is inferred from multiple signals and is not a manufacturer API assertion.");
                return "DigitalLicense";
            }
            if (type == "Retail") return "Retail product key";
            if (type == "Evaluation") return "Evaluation";
            return "Unknown";
        }
    }
}
