using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Runtime;
using LicenseScope.Core.Security;
using LicenseScope.Office.Models;

namespace LicenseScope.Office.Acquisition
{
    public interface IOsppEvidenceProvider { Task<OfficeCommandResult> ReadStatusAsync(OfficeToolLocation tool, CancellationToken cancellationToken); }
    public interface IVNextEvidenceProvider { Task<OfficeCommandResult> ReadStatusAsync(OfficeToolLocation tool, CancellationToken cancellationToken); }

    internal static class OfficeOutputSanitizer
    {
        private static readonly Regex Key = new Regex(@"(?i)\b(?:[A-Z0-9]{5}-){4}[A-Z0-9]{5}\b", RegexOptions.Compiled);
        private static readonly Regex Email = new Regex(@"(?i)\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b", RegexOptions.Compiled);
        private static readonly Regex Identifier = new Regex(@"(?i)\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b", RegexOptions.Compiled);
        public static string Sanitize(string value)
        {
            var noKeys = Key.Replace(value ?? string.Empty, m => SensitiveDataMasker.MaskProductKey(m.Value));
            var noEmails = Email.Replace(noKeys, m => SensitiveDataMasker.MaskEmail(m.Value));
            return Identifier.Replace(noEmails, "********-****-****-****-************");
        }
    }

    public sealed class OsppEvidenceProvider : IOsppEvidenceProvider
    {
        private readonly IProcessRunner _runner;
        public OsppEvidenceProvider(IProcessRunner runner) { _runner = runner ?? throw new ArgumentNullException(nameof(runner)); }
        public Task<OfficeCommandResult> ReadStatusAsync(OfficeToolLocation tool, CancellationToken cancellationToken) => RunFixedAsync(tool, "/dstatusall", cancellationToken);
        internal async Task<OfficeCommandResult> RunFixedAsync(OfficeToolLocation tool, string option, CancellationToken cancellationToken)
        {
            if (option != "/dstatus" && option != "/dstatusall") throw new ArgumentOutOfRangeException(nameof(option));
            if (tool == null || !string.Equals(tool.ToolType, "OSPP", StringComparison.Ordinal) || !Path.IsPathRooted(tool.FullPath) || !File.Exists(tool.FullPath)) return new OfficeCommandResult { Warning = "Official OSPP diagnostic tool not found." };
            var cscript = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cscript.exe");
            var execution = await _runner.RunAsync(new ProcessExecutionRequest { ExecutablePath = cscript, Arguments = "//nologo \"" + tool.FullPath + "\" " + option, WorkingDirectory = Path.GetDirectoryName(tool.FullPath) ?? string.Empty, Timeout = TimeSpan.FromSeconds(30), CreateNoWindow = true }, cancellationToken).ConfigureAwait(false);
            if (execution.WasCancelled) throw new OperationCanceledException(cancellationToken);
            if (execution.TimedOut) return new OfficeCommandResult { Warning = "OSPP diagnostic timed out." };
            if (execution.StartFailure || execution.ExitCode != 0) return new OfficeCommandResult { Warning = "OSPP diagnostic was unavailable." };
            return new OfficeCommandResult { Success = true, SanitizedOutput = OfficeOutputSanitizer.Sanitize(execution.StandardOutput) };
        }
    }

    public sealed class VNextEvidenceProvider : IVNextEvidenceProvider
    {
        private readonly IProcessRunner _runner;
        public VNextEvidenceProvider(IProcessRunner runner) { _runner = runner ?? throw new ArgumentNullException(nameof(runner)); }
        public async Task<OfficeCommandResult> ReadStatusAsync(OfficeToolLocation tool, CancellationToken cancellationToken)
        {
            if (tool == null || !string.Equals(tool.ToolType, "VNext", StringComparison.Ordinal) || !Path.IsPathRooted(tool.FullPath) || !File.Exists(tool.FullPath)) return new OfficeCommandResult { Warning = "Official vNext diagnostic tool not found." };
            var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
            var arguments = "-NoLogo -NoProfile -NonInteractive -File \"" + tool.FullPath + "\" -action list";
            var execution = await _runner.RunAsync(new ProcessExecutionRequest { ExecutablePath = powershell, Arguments = arguments, WorkingDirectory = Path.GetDirectoryName(tool.FullPath) ?? string.Empty, Timeout = TimeSpan.FromSeconds(30), CreateNoWindow = true }, cancellationToken).ConfigureAwait(false);
            if (execution.WasCancelled) throw new OperationCanceledException(cancellationToken);
            if (execution.TimedOut) return new OfficeCommandResult { Warning = "vNext diagnostic timed out." };
            if (execution.StartFailure || execution.ExitCode != 0) return new OfficeCommandResult { Warning = "vNext diagnostic was unavailable." };
            return new OfficeCommandResult { Success = true, SanitizedOutput = OfficeOutputSanitizer.Sanitize(execution.StandardOutput) };
        }
    }
}
