using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinLic.Core.Models;
using WinLic.Scanners.Office.Detection;
using WinLic.Scanners.Office.Models;
using WinLic.Scanners.Office.Parsing;

namespace WinLic.Scanners.Office.Acquisition
{
    public interface IOfficeEvidenceCollector { Task<OfficeEvidence> CollectAsync(SystemContext context, CancellationToken cancellationToken); }

    public sealed class OfficeEvidenceCollector : IOfficeEvidenceCollector
    {
        private readonly IOfficeInstallationDetector _detector; private readonly OfficeToolLocator _locator; private readonly IOsppEvidenceProvider _ospp; private readonly IVNextEvidenceProvider _vnext; private readonly IOsppStatusParser _osppParser; private readonly IVNextStatusParser _vnextParser;
        public OfficeEvidenceCollector(IOfficeInstallationDetector detector, OfficeToolLocator locator, IOsppEvidenceProvider ospp, IVNextEvidenceProvider vnext, IOsppStatusParser osppParser, IVNextStatusParser vnextParser) { _detector = detector; _locator = locator; _ospp = ospp; _vnext = vnext; _osppParser = osppParser; _vnextParser = vnextParser; }

        public async Task<OfficeEvidence> CollectAsync(SystemContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested(); var installs = _detector.Detect(); var tools = _locator.Locate(installs); var products = new List<OfficeProductEvidence>(); var warnings = new List<string>();
            var ospp = tools.FirstOrDefault(x => x.ToolType == "OSPP");
            if (ospp != null)
            {
                cancellationToken.ThrowIfCancellationRequested(); var result = await _ospp.ReadStatusAsync(ospp, cancellationToken).ConfigureAwait(false); if (result.Success) products.AddRange(_osppParser.Parse(result.SanitizedOutput, CultureInfo.CurrentCulture)); else if (result.Warning.Length > 0) warnings.Add(result.Warning);
            }
            else if (installs.Any(x => !x.UsesVNext)) warnings.Add("Official OSPP diagnostic tool not found.");
            var vnext = tools.FirstOrDefault(x => x.ToolType == "VNext");
            if (vnext != null && installs.Any(x => x.UsesVNext))
            {
                cancellationToken.ThrowIfCancellationRequested(); var result = await _vnext.ReadStatusAsync(vnext, cancellationToken).ConfigureAwait(false); if (result.Success) products.AddRange(_vnextParser.Parse(result.SanitizedOutput, CultureInfo.CurrentCulture)); else if (result.Warning.Length > 0) warnings.Add(result.Warning);
            }
            else if (installs.Any(x => x.UsesVNext)) warnings.Add("Official vNext diagnostic tool not found.");
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var install in installs)
            {
                var match = products.FirstOrDefault(x => x.Family == install.Family && (x.ProductId.IndexOf(install.ProductId, StringComparison.OrdinalIgnoreCase) >= 0 || install.ProductId.IndexOf(x.ProductId, StringComparison.OrdinalIgnoreCase) >= 0));
                if (match == null) products.Add(new OfficeProductEvidence { ProductId = install.ProductId, ProductName = install.ProductName, Family = install.Family, Version = install.Version, Architecture = install.Architecture, InstallationType = install.InstallationType, Channel = install.UsesVNext ? "Subscription" : string.Empty, LicenseState = install.UsesVNext ? "OnlineVerificationRequired" : string.Empty });
                else { match.Version = install.Version; match.Architecture = install.Architecture; match.InstallationType = install.InstallationType; }
            }
            return new OfficeEvidence { Installations = installs, Products = Deduplicate(products), Warnings = warnings };
        }

        private static IReadOnlyList<OfficeProductEvidence> Deduplicate(IEnumerable<OfficeProductEvidence> products) => products.GroupBy(x => (x.ProductId + "|" + x.Family).ToUpperInvariant()).Select(x => x.OrderByDescending(y => y.FromOfficialTool).First()).ToArray();
    }
}
