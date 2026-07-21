using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinLic.Application;
using WinLic.Compatibility;
using WinLic.Core.Models;
using WinLic.Reporting;

namespace WinLic.Cli
{
    public interface ICliConsole { void WriteLine(string value); }
    public sealed class SystemCliConsole : ICliConsole { public void WriteLine(string value)=>Console.WriteLine(value); }
    public static class Program
    {
        public static int Main(string[] args)=>Create().RunAsync(args,CancellationToken.None).GetAwaiter().GetResult();
        private static CliApplication Create(){var s=new AuditResultSanitizer();return new CliApplication(UnifiedAuditService.CreateProduction(),new IAuditReportWriter[]{new JsonAuditReportWriter(s),new CsvAuditReportWriter(s),new HtmlAuditReportWriter(s)},new SystemCliConsole());}
    }

    public sealed partial class CliApplication
    {
        private readonly IUnifiedAuditService _audit;private readonly IReadOnlyDictionary<string,IAuditReportWriter> _writers;private readonly ICliConsole _console;
        public CliApplication(IUnifiedAuditService audit,IEnumerable<IAuditReportWriter> writers,ICliConsole console){_audit=audit;_writers=writers.ToDictionary(x=>x.FormatId,StringComparer.OrdinalIgnoreCase);_console=console;}
        public async Task<int> RunAsync(string[] args,CancellationToken token)
        {
            try{if(args!=null&&args.Length>0&&string.Equals(args[0],"compatibility",StringComparison.OrdinalIgnoreCase))return PrintCompatibility(args);var parsed=Parse(args??Array.Empty<string>());if(parsed.Help){PrintHelp();return 0;}if(parsed.Version){_console.WriteLine(typeof(CliApplication).Assembly.GetName().Version?.ToString()??"1.0.0.0");return 0;}if(!parsed.Valid){_console.WriteLine("Error: "+parsed.Error);_console.WriteLine("Use --help for usage.");return 4;}var environment=new RuntimeEnvironmentDetector(new WindowsArchitectureProbe()).Detect();var assessment=new CompatibilityEvaluator().Evaluate(environment,CurrentPayload.Describe(environment));if(!assessment.CanRunAudit){foreach(var reason in assessment.BlockingReasons)_console.WriteLine("Compatibility error: "+reason);return environment.InstalledNetFrameworkVersion<new Version(4,8)?8:7;}var result=await _audit.RunAllAsync(token).ConfigureAwait(false);var consoleSnapshot=new AuditResultSanitizer().CreateReportSnapshot(result,new ReportWriteOptions{IncludeEvidence=false,IncludeWarnings=false});if(!parsed.Quiet){foreach(var p in consoleSnapshot.Products)_console.WriteLine(p.ScannerId+" | "+p.ProductName+" | "+p.Status+" | "+p.LicenseType);}
                foreach(var format in parsed.Formats){var writer=_writers[format];var path=Path.Combine(parsed.Output,DefaultName(format,result.CompletedAt));var written=await writer.WriteAsync(result,new ReportWriteOptions{OutputPath=path,IncludeEvidence=parsed.IncludeEvidence,IncludeWarnings=true,IncludeMachineName=parsed.IncludeMachine,Overwrite=parsed.Overwrite},token).ConfigureAwait(false);if(!written.Success){_console.WriteLine("Report failed: "+written.ErrorMessage);return 5;}if(!parsed.Quiet)_console.WriteLine("Report saved: "+written.OutputPath);}return ExitCode(result);
            }catch(OperationCanceledException){_console.WriteLine("Audit cancelled.");return 6;}catch(Exception ex){_console.WriteLine("Audit failed ("+ex.GetType().Name+").");return 3;}
        }
        public static int ExitCode(AuditResult result){if(result.WasCancelled)return 6;if(result.Products.Any(p=>p.Status==LicenseStatus.Unlicensed||p.Status==LicenseStatus.Expired))return 1;if(result.ScannerExecutions.Any(x=>!x.WasSuccessful)||result.Products.Any(p=>p.Status==LicenseStatus.Unknown||p.Status==LicenseStatus.Error||p.Status==LicenseStatus.NeedsSignIn||p.Status==LicenseStatus.NeedsOnlineVerification))return 2;return 0;}
        private static string DefaultName(string format,DateTimeOffset time)=>"WinLic-Audit-"+time.ToLocalTime().ToString("yyyyMMdd-HHmmss")+"."+format;
        private void PrintHelp(){_console.WriteLine("WinLicAudit.Cli — read-only license audit");_console.WriteLine("Usage: WinLicAudit.Cli.exe audit --all [options]");_console.WriteLine("       WinLicAudit.Cli.exe compatibility [--json]");_console.WriteLine("Options: --format json,csv,html  --output <directory>  --include-evidence|--no-evidence  --include-machine-name  --overwrite  --quiet  --help  --version");_console.WriteLine("Default output directory: .\\reports. Privacy defaults exclude machine name and mask keys/accounts.");_console.WriteLine("Exit codes: 0 compatible/clean, 1 unlicensed/expired, 2 incomplete, 3 fatal, 4 arguments, 5 report, 6 cancelled, 7 unsupported OS/architecture, 8 framework insufficient, 9 compatibility unknown.");}
        private static Args Parse(string[] args){var r=new Args();if(args.Length==1&&(args[0]=="--help"||args[0]=="-h")){r.Help=true;return r;}if(args.Length==1&&args[0]=="--version"){r.Version=true;return r;}if(args.Length<2||!string.Equals(args[0],"audit",StringComparison.OrdinalIgnoreCase)){r.Error="Expected 'audit --all'.";return r;}if(args.Contains("--help")){r.Help=true;return r;}if(!args.Contains("--all")){r.Error="The audit command requires --all.";return r;}r.Valid=true;for(var i=1;i<args.Length;i++){var a=args[i];if(a=="--all")continue;if(a=="--format"){if(++i>=args.Length){r.Valid=false;r.Error="Missing format value.";break;}foreach(var f in args[i].Split(',')){if(f!="json"&&f!="csv"&&f!="html"){r.Valid=false;r.Error="Invalid report format.";break;}if(!r.Formats.Contains(f))r.Formats.Add(f);}}else if(a=="--output"){if(++i>=args.Length){r.Valid=false;r.Error="Missing output path.";break;}r.Output=Path.GetFullPath(args[i]);}else if(a=="--include-evidence")r.IncludeEvidence=true;else if(a=="--no-evidence")r.IncludeEvidence=false;else if(a=="--include-machine-name")r.IncludeMachine=true;else if(a=="--overwrite")r.Overwrite=true;else if(a=="--quiet")r.Quiet=true;else{r.Valid=false;r.Error="Unknown option: "+a;break;}}return r;}
        private sealed class Args{public bool Valid;public bool Help;public bool Version;public string Error=string.Empty;public List<string> Formats=new List<string>();public string Output=Path.GetFullPath("reports");public bool IncludeEvidence=true;public bool IncludeMachine;public bool Overwrite;public bool Quiet;}
    }
}
