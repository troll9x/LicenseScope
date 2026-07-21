using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinLic.Core.Models;
using WinLic.Reporting;

namespace WinLic.Reporting.Tests
{
    [TestClass] public sealed class ReportingTests
    {
        [TestMethod] public void SanitizerDoesNotMutateAndMasksSensitiveData(){var source=Sample("AAAAA-BBBBB-CCCCC-DDDDD-ABCDE","person@example.com C:\\Users\\alice\\x");var snap=new AuditResultSanitizer().CreateReportSnapshot(source,new ReportWriteOptions());Assert.AreEqual("AAAAA-BBBBB-CCCCC-DDDDD-ABCDE",source.Products[0].PartialProductKey);Assert.AreEqual("XXXXX-XXXXX-XXXXX-XXXXX-ABCDE",snap.Products[0].PartialProductKey);Assert.IsFalse(snap.Products[0].Warnings[0].Contains("person@example.com"));StringAssert.Contains(snap.Products[0].Warnings[0],"<USER>");Assert.AreEqual(string.Empty,snap.System.MachineName);}
        [TestMethod] public void MachineNameIsPseudonymizedWhenIncluded(){var s=new AuditResultSanitizer().CreateReportSnapshot(Sample("",""),new ReportWriteOptions{IncludeMachineName=true});StringAssert.StartsWith(s.System.MachineName,"MACHINE-");}
        [TestMethod] public void SecretNamedEvidenceIsRemoved(){var a=Sample("","");a.Products[0].Evidence=new[]{new ScanEvidence{Name="AccessToken",Value="secret"}};Assert.AreEqual(0,new AuditResultSanitizer().CreateReportSnapshot(a,new ReportWriteOptions()).Products[0].Evidence.Count);}
        [TestMethod] public void SummaryDoesNotTreatUnknownAsUnlicensed(){var s=AuditSummary.From(new[]{new LicenseResult{Status=LicenseStatus.Unknown},new LicenseResult{Status=LicenseStatus.Licensed}});Assert.AreEqual(0,s.Unlicensed);Assert.AreEqual(1,s.Unknown);}
        [TestMethod] public void CsvNeutralizesFormulaAndUsesBom(){var p=Path.GetTempFileName();File.Delete(p);try{var a=Sample("","warn");a.Products[0].ProductName="=HYPERLINK(1)";var r=new CsvAuditReportWriter(new AuditResultSanitizer()).WriteAsync(a,new ReportWriteOptions{OutputPath=p},CancellationToken.None).Result;Assert.IsTrue(r.Success);var bytes=File.ReadAllBytes(p);Assert.AreEqual(0xEF,bytes[0]);StringAssert.Contains(File.ReadAllText(p,Encoding.UTF8),"'=HYPERLINK");}finally{if(File.Exists(p))File.Delete(p);}}
        [TestMethod] public void JsonHasSchemaEnumsAndIsoTime(){var text=Write(new JsonAuditReportWriter(new AuditResultSanitizer()),"json",Sample("",""));StringAssert.Contains(text,"\"schemaVersion\":\"1.0\"");StringAssert.Contains(text,"\"status\":\"Licensed\"");StringAssert.Contains(text,"2026-01-02T03:04:05");}
        [TestMethod] public void HtmlEscapesXssAndHasNoExternalResources(){var a=Sample("","");a.Products[0].ProductName="<script>alert(1)</script><img src=x>";var text=Write(new HtmlAuditReportWriter(new AuditResultSanitizer()),"html",a);Assert.IsFalse(text.Contains("<script>"));Assert.IsFalse(text.Contains("<img"));Assert.IsFalse(text.Contains("http://"));StringAssert.Contains(text,"&lt;script&gt;");}
        [TestMethod] public void WriterDoesNotOverwriteByDefault(){var p=Path.GetTempFileName();try{var r=new JsonAuditReportWriter(new AuditResultSanitizer()).WriteAsync(Sample("",""),new ReportWriteOptions{OutputPath=p},CancellationToken.None).Result;Assert.IsFalse(r.Success);}finally{File.Delete(p);}}
        [TestMethod] public void WriterCreatesDirectory(){var root=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));var p=Path.Combine(root,"a.json");try{Assert.IsTrue(new JsonAuditReportWriter(new AuditResultSanitizer()).WriteAsync(Sample("",""),new ReportWriteOptions{OutputPath=p},CancellationToken.None).Result.Success);}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
        [TestMethod] public void CancelledWriteLeavesNoReport(){var p=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".json");var c=new CancellationTokenSource();c.Cancel();var r=new JsonAuditReportWriter(new AuditResultSanitizer()).WriteAsync(Sample("",""),new ReportWriteOptions{OutputPath=p},c.Token).Result;Assert.IsFalse(r.Success);Assert.IsFalse(File.Exists(p));}
        private static string Write(IAuditReportWriter w,string ext,AuditResult a){var p=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+"."+ext);try{Assert.IsTrue(w.WriteAsync(a,new ReportWriteOptions{OutputPath=p},CancellationToken.None).Result.Success);return File.ReadAllText(p);}finally{if(File.Exists(p))File.Delete(p);}}
        private static AuditResult Sample(string key,string warning)=>new AuditResult{System=new SystemContext{MachineName="DESKTOP-SECRET",OsName="Windows 10"},StartedAt=new DateTimeOffset(2026,1,2,3,4,5,TimeSpan.Zero),CompletedAt=new DateTimeOffset(2026,1,2,3,4,6,TimeSpan.Zero),Products=new[]{new LicenseResult{ScannerId="microsoft.windows",Vendor="Microsoft",ProductName="Windows",Installed=true,Status=LicenseStatus.Licensed,IsLicensed=true,PartialProductKey=key,Warnings=new[]{warning}}},ScannerExecutions=new[]{new ScannerExecutionResult{ScannerId="microsoft.windows",WasSuccessful=true}}};
    }
}
