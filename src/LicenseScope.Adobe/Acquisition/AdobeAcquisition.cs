using System; using System.Diagnostics; using System.IO; using System.ServiceProcess; using System.Threading; using System.Threading.Tasks; using LicenseScope.Core.Contracts; using LicenseScope.Core.Runtime; using LicenseScope.Adobe.Contracts; using LicenseScope.Adobe.Models; using LicenseScope.Adobe.Parsing;
namespace LicenseScope.Adobe.Acquisition
{
 public sealed class AdobeLicensingToolkitLocator:IAdobeLicensingToolkitLocator
 {
  const string FileName="adobe-licensing-toolkit.exe"; public AdobeToolkitInfo Locate(){var root=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tools","adobe"));var path=Path.GetFullPath(Path.Combine(root,FileName));var trusted=path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&string.Equals(Path.GetFileName(path),FileName,StringComparison.OrdinalIgnoreCase);var exists=trusted&&File.Exists(path);return new AdobeToolkitInfo{Path=path,Exists=exists,Version=exists?FileVersionInfo.GetVersionInfo(path).FileVersion??"":""};}
 }
 public sealed class AdobeLicensingEvidenceProvider:IAdobeLicensingEvidenceProvider
 {
  const string InformationArgument="--licenseInformation";readonly IProcessRunner runner;readonly IAdobeLicensingToolkitLocator locator;readonly AdobeLicenseInformationParser parser;public AdobeLicensingEvidenceProvider(IProcessRunner r,IAdobeLicensingToolkitLocator l,AdobeLicenseInformationParser p){runner=r;locator=l;parser=p;}
  public async Task<AdobeLicenseInformationResult> ReadLicenseInformationAsync(CancellationToken token){var tool=locator.Locate();if(!tool.Exists)return Fail("Adobe Licensing Toolkit not found in the trusted application path.");var request=new ProcessExecutionRequest{ExecutablePath=tool.Path,Arguments=InformationArgument,WorkingDirectory=Path.GetDirectoryName(tool.Path)??"",CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,Timeout=TimeSpan.FromSeconds(30)};var result=await runner.RunAsync(request,token).ConfigureAwait(false);if(result.WasCancelled)throw new OperationCanceledException(token);if(result.TimedOut)return Fail("Adobe Licensing Toolkit timed out.");if(result.StartFailure)return Fail("Adobe Licensing Toolkit could not start.");if(result.ExitCode!=0)return Fail("Adobe Licensing Toolkit returned a non-zero exit code.");var parsed=parser.Parse(result.StandardOutput);if(!string.IsNullOrWhiteSpace(result.StandardError)){var warnings=new System.Collections.Generic.List<string>(parsed.Warnings){"Adobe Licensing Toolkit returned a diagnostic message; raw text was suppressed."};parsed.Warnings=warnings;}return parsed;}
  static AdobeLicenseInformationResult Fail(string warning)=>new AdobeLicenseInformationResult{Warnings=new[]{warning}};
 }
 public sealed class AdobeServiceStatusProvider:IAdobeServiceStatusProvider
 {
  public AdobeServiceStatus GetStatus(){foreach(var name in new[]{"AGSService","AdobeARMservice"})try{using(var service=new ServiceController(name)){var status=service.Status;return new AdobeServiceStatus{Found=true,Running=status==ServiceControllerStatus.Running};}}catch(InvalidOperationException){continue;}catch(System.ComponentModel.Win32Exception ex){return new AdobeServiceStatus{Warning="Adobe service status unavailable: "+ex.NativeErrorCode};}return new AdobeServiceStatus{Warning="Adobe Genuine Service not found."};}
 }
}
