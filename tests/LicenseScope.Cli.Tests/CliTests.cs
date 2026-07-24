using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LicenseScope.Application;
using LicenseScope.Cli;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;
using LicenseScope.Reporting;

namespace LicenseScope.Cli.Tests
{
    [TestClass] public sealed class CliTests
    {
        [TestMethod][DataRow(new string[0],4)][DataRow(new[]{"--help"},0)][DataRow(new[]{"--version"},0)][DataRow(new[]{"audit","--help"},0)][DataRow(new[]{"audit"},4)][DataRow(new[]{"audit","--all","--bad"},4)][DataRow(new[]{"audit","--all","--format","pdf"},4)]
        public async Task ArgumentAndHelpExitCodes(string[] args,int expected){Assert.AreEqual(expected,await App(Clean()).RunAsync(args,CancellationToken.None));}
        [TestMethod][DataRow(LicenseStatus.Licensed,0)][DataRow(LicenseStatus.Unlicensed,1)][DataRow(LicenseStatus.Expired,1)][DataRow(LicenseStatus.Unknown,2)][DataRow(LicenseStatus.NeedsSignIn,2)][DataRow(LicenseStatus.NeedsOnlineVerification,2)][DataRow(LicenseStatus.NotInstalled,0)]
        public async Task StatusExitCodes(LicenseStatus status,int expected){Assert.AreEqual(expected,await App(Result(status)).RunAsync(new[]{"audit","--all","--quiet"},CancellationToken.None));}
        [TestMethod] public async Task MultipleFormatsRunScannerOnce(){var root=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));var service=new FakeAudit(Clean());try{var code=await App(Clean(),service).RunAsync(new[]{"audit","--all","--format","json,csv,html","--output",root},CancellationToken.None);Assert.AreEqual(0,code);Assert.AreEqual(1,service.Calls);Assert.AreEqual(3,Directory.GetFiles(root).Length);}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
        [TestMethod] public async Task ScannerErrorReturnsTwo(){var a=Clean();a.ScannerExecutions=new[]{new ScannerExecutionResult{ScannerId="x",WasSuccessful=false}};Assert.AreEqual(2,await App(a).RunAsync(new[]{"audit","--all"},CancellationToken.None));}
        [TestMethod] public async Task ConsoleMasksRawEmail(){var a=Clean();a.Products[0].ProductName="person@example.com";var console=new BufferConsole();var s=new AuditResultSanitizer();var app=new CliApplication(new FakeAudit(a),new IAuditReportWriter[]{new JsonAuditReportWriter(s)},console);await app.RunAsync(new[]{"audit","--all"},CancellationToken.None);Assert.IsFalse(string.Join("\n",console.Lines).Contains("person@example.com"));}
        [TestMethod] public void ProductionIdsAreStableAndUnique(){var f=new[]{new FakeScanner("microsoft.windows"),new FakeScanner("microsoft.office")};ProductionScannerFactory.ValidateUnique(f);Assert.ThrowsExactly<ArgumentException>(()=>ProductionScannerFactory.ValidateUnique(new[]{new FakeScanner("x"),new FakeScanner("x")}));}
        [TestMethod] public async Task CompatibilityJsonIsSanitizedAndDoesNotAudit(){var service=new FakeAudit(Clean());var console=new BufferConsole();var s=new AuditResultSanitizer();var app=new CliApplication(service,new IAuditReportWriter[]{new JsonAuditReportWriter(s)},console);Assert.AreEqual(0,await app.RunAsync(new[]{"compatibility","--json"},CancellationToken.None));Assert.AreEqual(0,service.Calls);var output=string.Join("",console.Lines);StringAssert.Contains(output,"\"nativeArchitecture\"");Assert.IsFalse(output.Contains(Environment.MachineName));}
        [TestMethod] public async Task CompatibilityRejectsUnknownOption(){Assert.AreEqual(4,await App(Clean()).RunAsync(new[]{"compatibility","--bad"},CancellationToken.None));}
        [TestMethod] public async Task DeepForensicScanRequiresExplicitConsent(){Assert.AreEqual(4,await App(Clean()).RunAsync(new[]{"audit","--all","--deep-forensic-scan"},CancellationToken.None));}
        [TestMethod] public async Task DeepForensicConsentIsForwarded(){var service=new FakeAudit(Clean());Assert.AreEqual(0,await App(Clean(),service).RunAsync(new[]{"audit","--all","--deep-forensic-scan","--consent-forensic-read","--quiet"},CancellationToken.None));Assert.IsNotNull(service.Options);Assert.IsTrue(service.Options.DeepForensicScan);Assert.IsTrue(service.Options.UserConsented);}
        [TestMethod] public async Task ConsoleUsesVietnameseStructuredBinaryTraceFacts(){var audit=Clean();audit.CrackTraceAnalysis=new CrackTraceAnalysisResult{ScanCompleted=true,ActivationDetected=true,TraceDetected=false,ProvenanceVerified=false,TraceVerdict=CrackTraceVerdict.TraceNotFound,VerdictSummary="KHÔNG PHÁT HIỆN DẤU VẾT",DetectionCoverage=new[]{new DetectionCoverageItem{Id="current",DisplayName="Trạng thái cấp phép hiện tại",Status=DetectionCoverageStatus.Checked,Checked=true}}};var console=new BufferConsole();var sanitizer=new AuditResultSanitizer();var app=new CliApplication(new FakeAudit(audit),new IAuditReportWriter[]{new JsonAuditReportWriter(sanitizer)},console);Assert.AreEqual(0,await app.RunAsync(new[]{"audit","--all"},CancellationToken.None));var text=string.Join("\n",console.Lines);StringAssert.Contains(text,"Quét hoàn tất: CÓ");StringAssert.Contains(text,"Phát hiện dấu vết: KHÔNG");Assert.IsFalse(text.Contains("ScanCompleted:"));Assert.IsFalse(text.Contains("TraceDetected:"));Assert.IsFalse(text.Contains("INCONCLUSIVE"));}
        private static CliApplication App(AuditResult a,FakeAudit? service=null){var s=new AuditResultSanitizer();return new CliApplication(service??new FakeAudit(a),new IAuditReportWriter[]{new JsonAuditReportWriter(s),new CsvAuditReportWriter(s),new HtmlAuditReportWriter(s)},new BufferConsole());}
        private static AuditResult Clean()=>Result(LicenseStatus.Licensed);
        private static AuditResult Result(LicenseStatus status)=>new AuditResult{StartedAt=DateTimeOffset.UtcNow,CompletedAt=DateTimeOffset.UtcNow,Products=new[]{new LicenseResult{ScannerId="test",ProductName="Product",Status=status}},ScannerExecutions=new[]{new ScannerExecutionResult{ScannerId="test",WasSuccessful=true}}};
        private sealed class FakeAudit:IUnifiedAuditService{private readonly AuditResult _r;public int Calls;public CrackTraceScanOptions? Options;public FakeAudit(AuditResult r){_r=r;}public Task<AuditResult> RunAllAsync(CancellationToken t,IProgress<AuditProgress>? p=null,CrackTraceScanOptions? options=null){Calls++;Options=options;return Task.FromResult(_r);}}
        private sealed class BufferConsole:ICliConsole{public readonly List<string> Lines=new List<string>();public void WriteLine(string v)=>Lines.Add(v);}
        private sealed class FakeScanner:ILicenseScanner{public FakeScanner(string id){ScannerId=id;}public string ScannerId{get;}public string VendorName=>"Test";public bool IsApplicable(SystemContext c)=>true;public Task<IReadOnlyList<LicenseResult>> ScanAsync(SystemContext c,CancellationToken t)=>Task.FromResult((IReadOnlyList<LicenseResult>)Array.Empty<LicenseResult>());}
    }
}
