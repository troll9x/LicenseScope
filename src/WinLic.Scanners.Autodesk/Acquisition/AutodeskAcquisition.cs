using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using WinLic.Core.Contracts;
using WinLic.Core.Models;
using WinLic.Core.Runtime;
using WinLic.Scanners.Autodesk.Contracts;
using WinLic.Scanners.Autodesk.Models;
using WinLic.Scanners.Autodesk.Parsing;

namespace WinLic.Scanners.Autodesk.Acquisition
{
    public sealed class AutodeskLicensingToolLocator : IAutodeskLicensingToolLocator
    {
        public AutodeskToolInfo Locate()
        {
            var root = Environment.GetEnvironmentVariable("CommonProgramFiles(x86)") ?? "";
            var path = Path.Combine(root, "Autodesk Shared", "AdskLicensing", "Current", "helper", "AdskLicensingInstHelper.exe");
            var exists = !string.IsNullOrWhiteSpace(root) && File.Exists(path); var version = exists ? FileVersionInfo.GetVersionInfo(path).FileVersion ?? "" : "";
            return new AutodeskToolInfo { Path = path, Exists = exists, Version = version };
        }
    }
    public sealed class AutodeskLicensingEvidenceProvider : IAutodeskLicensingEvidenceProvider
    {
        private const string ListArgument = "list"; private readonly IProcessRunner _runner; private readonly IAutodeskLicensingToolLocator _locator; private readonly AdskLicensingListParser _parser;
        public AutodeskLicensingEvidenceProvider(IProcessRunner runner, IAutodeskLicensingToolLocator locator, AdskLicensingListParser parser) { _runner = runner; _locator = locator; _parser = parser; }
        public async Task<AutodeskEvidenceResult> ListAsync(CancellationToken cancellationToken)
        {
            var tool = _locator.Locate(); if (!tool.Exists) return Failure("Autodesk Licensing Helper not found.");
            var request = new ProcessExecutionRequest { ExecutablePath = tool.Path, Arguments = ListArgument, WorkingDirectory = Path.GetDirectoryName(tool.Path) ?? "", CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, Timeout = TimeSpan.FromSeconds(30) };
            var result = await _runner.RunAsync(request, cancellationToken).ConfigureAwait(false); if (result.WasCancelled) throw new OperationCanceledException(cancellationToken);
            if (result.TimedOut) return Failure("Autodesk Licensing Helper timed out."); if (result.StartFailure) return Failure("Autodesk Licensing Helper could not start."); if (result.ExitCode != 0) return Failure("Autodesk Licensing Helper returned a non-zero exit code.");
            return _parser.Parse(result.StandardOutput);
        }
        private static AutodeskEvidenceResult Failure(string warning) => new AutodeskEvidenceResult { Warnings = new[] { warning } };
    }
    public sealed class AutodeskServiceStatusProvider : IAutodeskServiceStatusProvider
    {
        public AutodeskServiceStatus GetStatus()
        {
            try { using (var service = new ServiceController("AdskLicensingService")) { var status = service.Status; return new AutodeskServiceStatus { Found = true, Running = status == ServiceControllerStatus.Running }; } }
            catch (InvalidOperationException) { return new AutodeskServiceStatus { Warning = "Autodesk Desktop Licensing Service not found." }; }
            catch (System.ComponentModel.Win32Exception ex) { return new AutodeskServiceStatus { Warning = "Autodesk licensing service status unavailable: " + ex.NativeErrorCode }; }
        }
    }
}
