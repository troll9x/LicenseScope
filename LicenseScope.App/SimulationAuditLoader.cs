using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using LicenseScope.Core.Models;
using LicenseScope.Core.Runtime;
using LicenseScope.Core.Services;

namespace LicenseScope.App
{
    [DataContract]
    internal sealed class SimulationAuditFile
    {
        [DataMember(Name = "schemaVersion")]
        public int SchemaVersion { get; set; }

        [DataMember(Name = "products")]
        public List<SimulationProduct> Products { get; set; } = new List<SimulationProduct>();
    }

    [DataContract]
    internal sealed class SimulationProduct
    {
        [DataMember(Name = "scannerId")]
        public string ScannerId { get; set; } = string.Empty;

        [DataMember(Name = "vendor")]
        public string Vendor { get; set; } = string.Empty;

        [DataMember(Name = "productName")]
        public string ProductName { get; set; } = string.Empty;

        [DataMember(Name = "productVersion")]
        public string ProductVersion { get; set; } = string.Empty;

        [DataMember(Name = "status")]
        public string Status { get; set; } = string.Empty;

        [DataMember(Name = "licenseType")]
        public string LicenseType { get; set; } = string.Empty;

        [DataMember(Name = "confidence")]
        public string Confidence { get; set; } = string.Empty;

        [DataMember(Name = "expirationDate")]
        public string ExpirationDate { get; set; } = string.Empty;

        [DataMember(Name = "warnings")]
        public List<string> Warnings { get; set; } = new List<string>();
    }

    internal sealed class SimulationAuditLoader
    {
        public const string SimulationMarker = "SIMULATION";
        private const long MaximumFileSize = 1024 * 1024;

        private static readonly IReadOnlyDictionary<string, string> AllowedScanners =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["adobe.desktop"] = "Adobe",
                ["autodesk.desktop"] = "Autodesk",
                ["trimble.sketchup"] = "Trimble"
            };

        public AuditResult Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Chưa chọn tệp giả lập.", nameof(path));
            var file = new FileInfo(path);
            if (!file.Exists) throw new FileNotFoundException("Không tìm thấy tệp giả lập.", path);
            if (file.Length <= 0 || file.Length > MaximumFileSize)
                throw new InvalidDataException("Tệp giả lập phải có kích thước từ 1 byte đến 1 MB.");

            SimulationAuditFile? document;
            var serializer = new DataContractJsonSerializer(typeof(SimulationAuditFile));
            using (var stream = file.OpenRead())
                document = serializer.ReadObject(stream) as SimulationAuditFile;

            if (document == null || document.SchemaVersion != 1)
                throw new InvalidDataException("Phiên bản định dạng giả lập không được hỗ trợ.");
            if (document.Products == null || document.Products.Count == 0 || document.Products.Count > 100)
                throw new InvalidDataException("Tệp giả lập phải chứa từ 1 đến 100 sản phẩm.");

            var products = document.Products.Select(CreateResult).ToArray();
            var now = DateTimeOffset.Now;
            return new AuditResult
            {
                System = new DefaultSystemContextProvider().GetCurrent(),
                StartedAt = now,
                CompletedAt = now,
                Products = products,
                ScannerExecutions = products
                    .GroupBy(x => x.ScannerId, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new ScannerExecutionResult
                    {
                        ScannerId = group.Key,
                        StartedAt = now,
                        CompletedAt = now,
                        WasApplicable = true,
                        WasSuccessful = true,
                        ProductResultCount = group.Count()
                    })
                    .ToArray()
            };
        }

        private static LicenseResult CreateResult(SimulationProduct input)
        {
            string expectedVendor;
            if (!AllowedScanners.TryGetValue(input.ScannerId ?? string.Empty, out expectedVendor))
                throw new InvalidDataException("Bộ quét giả lập không hợp lệ: " + input.ScannerId);
            if (string.IsNullOrWhiteSpace(input.ProductName))
                throw new InvalidDataException("Tên sản phẩm giả lập không được để trống.");

            LicenseStatus status;
            if (!Enum.TryParse(input.Status, true, out status) || status == LicenseStatus.NotInstalled)
                throw new InvalidDataException("Trạng thái giả lập không hợp lệ: " + input.Status);

            ConfidenceLevel confidence;
            if (!Enum.TryParse(input.Confidence, true, out confidence))
                confidence = ConfidenceLevel.Low;

            DateTimeOffset expiration;
            DateTimeOffset? expirationDate = DateTimeOffset.TryParse(input.ExpirationDate, out expiration)
                ? expiration
                : (DateTimeOffset?)null;
            var warnings = new List<string>
            {
                "DỮ LIỆU MÔ PHỎNG: kết quả này không phản ánh phần mềm hoặc bản quyền thật trên máy."
            };
            warnings.AddRange((input.Warnings ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()));

            return new LicenseResult
            {
                ScannerId = (input.ScannerId ?? string.Empty).Trim(),
                Vendor = string.IsNullOrWhiteSpace(input.Vendor) ? expectedVendor : input.Vendor.Trim(),
                ProductName = (input.ProductName ?? string.Empty).Trim(),
                ProductVersion = (input.ProductVersion ?? string.Empty).Trim(),
                Installed = true,
                Status = status,
                IsLicensed = LicenseStatusMapper.ToIsLicensed(status),
                LicenseType = (input.LicenseType ?? string.Empty).Trim(),
                Confidence = confidence,
                ExpirationDate = expirationDate,
                ErrorCode = SimulationMarker,
                Evidence = new[]
                {
                    new ScanEvidence
                    {
                        Source = "Tệp giả lập",
                        Name = "Chế độ",
                        Value = "Mô phỏng — không đọc Registry hoặc dữ liệu bản quyền thật",
                        Confidence = ConfidenceLevel.High
                    }
                },
                Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            };
        }
    }
}
