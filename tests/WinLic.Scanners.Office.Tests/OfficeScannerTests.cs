using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinLic.Core.Models;
using WinLic.Scanners.Office.Acquisition;
using WinLic.Scanners.Office.Classification;
using WinLic.Scanners.Office.Mapping;
using WinLic.Scanners.Office.Models;

namespace WinLic.Scanners.Office.Tests
{
    [TestClass] public sealed class OfficeScannerTests
    {
        [TestMethod] public async Task ReturnsSeparateOfficeProjectAndVisioResults() { var scanner = Create(new OfficeProductEvidence { ProductId="Office",ProductName="Office",Family=OfficeProductFamily.OfficeSuite,LicenseState="LICENSED",FromOfficialTool=true }, new OfficeProductEvidence { ProductId="Project",ProductName="Project",Family=OfficeProductFamily.Project,LicenseState="LICENSED",FromOfficialTool=true }, new OfficeProductEvidence { ProductId="Visio",ProductName="Visio",Family=OfficeProductFamily.Visio,LicenseState="UNLICENSED",FromOfficialTool=true }); var result=await scanner.ScanAsync(Context(),CancellationToken.None); Assert.AreEqual(3,result.Count); Assert.AreEqual(LicenseStatus.Unlicensed,result[2].Status); }
        [TestMethod] public async Task ResultCollectionIsNeverNull() { Assert.IsNotNull(await Create().ScanAsync(Context(), CancellationToken.None)); }
        [TestMethod] public async Task CancellationPropagates() { var c=new CancellationTokenSource(); c.Cancel(); await Assert.ThrowsExactlyAsync<OperationCanceledException>(()=>Create().ScanAsync(Context(),c.Token)); }
        [TestMethod] public async Task FullKeyIsMaskedInResult() { var p=new OfficeProductEvidence { ProductId="Office",ProductName="Office",PartialProductKey="AAAAA-BBBBB-CCCCC-DDDDD-ABCDE",FromOfficialTool=true,LicenseState="LICENSED" }; var r=(await Create(p).ScanAsync(Context(),CancellationToken.None))[0]; Assert.AreEqual("XXXXX-XXXXX-XXXXX-XXXXX-ABCDE",r.PartialProductKey); }
        [TestMethod] public async Task M365UnknownIsNotUnlicensed() { var p=new OfficeProductEvidence { ProductId="O365",ProductName="Microsoft 365 Apps",Family=OfficeProductFamily.Microsoft365Apps }; var r=(await Create(p).ScanAsync(Context(),CancellationToken.None))[0]; Assert.AreEqual(LicenseStatus.NeedsOnlineVerification,r.Status); Assert.IsNull(r.IsLicensed); }
        [TestMethod] public void ScannerIdentityIsStable() { Assert.AreEqual("microsoft.office",Create().ScannerId); Assert.IsTrue(Create().IsApplicable(Context())); }
        private static OfficeLicenseScanner Create(params OfficeProductEvidence[] products) => new OfficeLicenseScanner(new FakeCollector(products),new OfficeLicenseResultFactory(new OfficeLicenseClassifier()));
        private static SystemContext Context()=>new SystemContext { OsName="Microsoft Windows",WindowsDirectory=@"C:\Windows" };
        private sealed class FakeCollector : IOfficeEvidenceCollector { private readonly OfficeProductEvidence[] _p; public FakeCollector(OfficeProductEvidence[] p){_p=p;} public Task<OfficeEvidence> CollectAsync(SystemContext c,CancellationToken t){t.ThrowIfCancellationRequested();return Task.FromResult(new OfficeEvidence { Products=_p });} }
    }
}
