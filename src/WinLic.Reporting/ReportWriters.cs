using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinLic.Core.Models;

namespace WinLic.Reporting
{
    internal static class ReportFile
    {
        public static async Task<ReportWriteResult> WriteAsync(string path,string content,bool overwrite,CancellationToken token,bool bom=false)
        {
            try { if(string.IsNullOrWhiteSpace(path))return Fail("Output path is required.");var full=Path.GetFullPath(path);var dir=Path.GetDirectoryName(full);if(string.IsNullOrWhiteSpace(dir))return Fail("Invalid output path.");Directory.CreateDirectory(dir);if(File.Exists(full)&&!overwrite)return Fail("Output file already exists.");var temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";try{token.ThrowIfCancellationRequested();var encoding=new UTF8Encoding(bom);using(var writer=new StreamWriter(temp,false,encoding)){await writer.WriteAsync(content).ConfigureAwait(false);}token.ThrowIfCancellationRequested();if(File.Exists(full))File.Delete(full);File.Move(temp,full);return new ReportWriteResult{Success=true,OutputPath=full};}finally{if(File.Exists(temp))File.Delete(temp);}}
            catch(OperationCanceledException){return Fail("Report writing was cancelled.");}catch(Exception ex){return Fail("Report could not be written ("+ex.GetType().Name+").");}
        }
        private static ReportWriteResult Fail(string message)=>new ReportWriteResult{ErrorMessage=message};
    }

    internal static class Escape
    {
        public static string Json(string value)=>"\""+(value??string.Empty).Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n").Replace("\t","\\t")+"\"";
        public static string Html(string value)=>(value??string.Empty).Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;").Replace("'","&#39;");
        public static string Csv(string value){var v=value??string.Empty;if(v.Length>0&&(v[0]=='='||v[0]=='+'||v[0]=='-'||v[0]=='@'||v[0]=='\t'||v[0]=='\r'))v="'"+v;return "\""+v.Replace("\"","\"\"")+"\"";}
    }

    public abstract class AuditReportWriterBase : IAuditReportWriter
    {
        private readonly IAuditResultSanitizer _sanitizer; protected AuditReportWriterBase(IAuditResultSanitizer sanitizer){_sanitizer=sanitizer;}public abstract string FormatId{get;} protected abstract string Render(SanitizedAuditReport report,ReportWriteOptions options);
        public Task<ReportWriteResult> WriteAsync(AuditResult result,ReportWriteOptions options,CancellationToken token){if(options==null)return Task.FromResult(new ReportWriteResult{ErrorMessage="Options are required."});var snapshot=_sanitizer.CreateReportSnapshot(result,options);return ReportFile.WriteAsync(options.OutputPath,Render(snapshot,options),options.Overwrite,token,FormatId=="csv");}
    }

    public sealed class JsonAuditReportWriter : AuditReportWriterBase
    {
        public JsonAuditReportWriter(IAuditResultSanitizer s):base(s){} public override string FormatId=>"json";
        protected override string Render(SanitizedAuditReport r,ReportWriteOptions o){var s=AuditSummary.From(r.Products);var products=string.Join(",",r.Products.Select(p=>"{\"scannerId\":"+Escape.Json(p.ScannerId)+",\"vendor\":"+Escape.Json(p.Vendor)+",\"productName\":"+Escape.Json(p.ProductName)+",\"productVersion\":"+Escape.Json(p.ProductVersion)+",\"installed\":"+p.Installed.ToString().ToLowerInvariant()+",\"status\":"+Escape.Json(p.Status.ToString())+",\"isLicensed\":"+(p.IsLicensed.HasValue?p.IsLicensed.Value.ToString().ToLowerInvariant():"null")+",\"licenseType\":"+Escape.Json(p.LicenseType)+",\"partialProductKey\":"+Escape.Json(p.PartialProductKey)+",\"expirationDate\":"+(p.ExpirationDate.HasValue?Escape.Json(p.ExpirationDate.Value.ToString("o")):"null")+",\"confidence\":"+Escape.Json(p.Confidence.ToString())+",\"warnings\":["+string.Join(",",p.Warnings.Select(Escape.Json))+"],\"evidence\":["+string.Join(",",p.Evidence.Select(e=>"{\"source\":"+Escape.Json(e.Source)+",\"name\":"+Escape.Json(e.Name)+",\"value\":"+Escape.Json(e.Value)+"}"))+"]}"));var exec=string.Join(",",r.ScannerExecutions.Select(x=>"{\"scannerId\":"+Escape.Json(x.ScannerId)+",\"successful\":"+x.WasSuccessful.ToString().ToLowerInvariant()+",\"cancelled\":"+x.WasCancelled.ToString().ToLowerInvariant()+",\"productCount\":"+x.ProductResultCount+",\"error\":"+Escape.Json(x.ErrorMessage)+"}"));return "{\"schemaVersion\":\"1.0\",\"application\":{\"name\":\"WinLic Audit\",\"version\":"+Escape.Json(r.ApplicationVersion)+"},\"audit\":{\"startedAt\":"+Escape.Json(r.StartedAt.ToString("o"))+",\"completedAt\":"+Escape.Json(r.CompletedAt.ToString("o"))+",\"durationMilliseconds\":"+(long)(r.CompletedAt-r.StartedAt).TotalMilliseconds+",\"wasCancelled\":"+r.WasCancelled.ToString().ToLowerInvariant()+"},\"system\":{\"operatingSystem\":"+Escape.Json(r.System.OsName)+",\"version\":"+Escape.Json(r.System.OsVersion)+",\"build\":"+Escape.Json(r.System.OsBuild)+",\"architecture\":"+Escape.Json(r.System.OsArchitecture.ToString())+",\"machine\":"+(string.IsNullOrEmpty(r.System.MachineName)?"null":Escape.Json(r.System.MachineName))+"},\"summary\":{\"total\":"+s.Total+",\"licensed\":"+s.Licensed+",\"unlicensed\":"+s.Unlicensed+",\"expired\":"+s.Expired+",\"attention\":"+s.Attention+",\"unknown\":"+s.Unknown+"},\"products\":["+products+"],\"scannerExecutions\":["+exec+"]}";}
    }

    public sealed class CsvAuditReportWriter : AuditReportWriterBase
    {
        public CsvAuditReportWriter(IAuditResultSanitizer s):base(s){}public override string FormatId=>"csv";
        protected override string Render(SanitizedAuditReport r,ReportWriteOptions o){var b=new StringBuilder("ScanTime,ScannerId,Vendor,ProductName,ProductVersion,Installed,Status,IsLicensed,LicenseType,ExpirationDate,Confidence,Warnings\r\n");foreach(var p in r.Products)b.AppendLine(string.Join(",",new[]{Escape.Csv(r.CompletedAt.ToString("o")),Escape.Csv(p.ScannerId),Escape.Csv(p.Vendor),Escape.Csv(p.ProductName),Escape.Csv(p.ProductVersion),Escape.Csv(p.Installed.ToString()),Escape.Csv(p.Status.ToString()),Escape.Csv(p.IsLicensed?.ToString()??string.Empty),Escape.Csv(p.LicenseType),Escape.Csv(p.ExpirationDate?.ToString("o")??string.Empty),Escape.Csv(p.Confidence.ToString()),Escape.Csv(string.Join(" | ",p.Warnings))}));return b.ToString();}
    }

    public sealed class HtmlAuditReportWriter : AuditReportWriterBase
    {
        public HtmlAuditReportWriter(IAuditResultSanitizer s):base(s){}public override string FormatId=>"html";
        protected override string Render(SanitizedAuditReport r,ReportWriteOptions o){var s=AuditSummary.From(r.Products);var rows=string.Join("",r.Products.Select(p=>"<tr><td>"+Escape.Html(p.Vendor)+"</td><td>"+Escape.Html(p.ProductName)+"</td><td>"+Escape.Html(p.ProductVersion)+"</td><td>"+Escape.Html(p.Status.ToString())+"</td><td>"+Escape.Html(p.LicenseType)+"</td><td>"+Escape.Html(p.Confidence.ToString())+"</td><td>"+Escape.Html(string.Join("; ",p.Warnings))+"</td></tr>"));var exec=string.Join("",r.ScannerExecutions.Select(x=>"<li>"+Escape.Html(x.ScannerId)+": "+(x.WasSuccessful?"Success":"Failed")+" "+Escape.Html(x.ErrorMessage)+"</li>"));return "<!doctype html><html><head><meta charset=\"utf-8\"><title>WinLic Audit</title><style>body{font-family:Segoe UI,sans-serif;margin:24px;color:#242038}.cards{display:flex;gap:12px}.card{padding:12px;border:1px solid #ddd;border-radius:8px}table{border-collapse:collapse;width:100%;margin-top:18px}th,td{border:1px solid #ddd;padding:8px;text-align:left}th{background:#f3f0fe}@media print{body{margin:0}}</style></head><body><h1>WinLic Audit</h1><p>Completed: "+Escape.Html(r.CompletedAt.ToString("o"))+(r.WasCancelled?" — Cancelled":"")+"</p><div class=\"cards\"><div class=\"card\">Total: "+s.Total+"</div><div class=\"card\">Licensed: "+s.Licensed+"</div><div class=\"card\">Attention: "+s.Attention+"</div></div><table><thead><tr><th>Vendor</th><th>Product</th><th>Version</th><th>Status</th><th>License type</th><th>Confidence</th><th>Warnings</th></tr></thead><tbody>"+rows+"</tbody></table><h2>Scanner executions</h2><ul>"+exec+"</ul></body></html>";}
    }
}
