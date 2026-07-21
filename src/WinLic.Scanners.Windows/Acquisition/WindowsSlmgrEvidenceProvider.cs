using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WinLic.Core.Contracts;
using WinLic.Core.Runtime;
using WinLic.Scanners.Windows.Models;
using WinLic.Scanners.Windows.Parsing;

namespace WinLic.Scanners.Windows.Acquisition
{
    public sealed class WindowsSlmgrEvidenceProvider
    {
        private readonly IProcessRunner _runner;
        private readonly ISlmgrXprParser _xprParser;
        private readonly ISlmgrDlvParser _dlvParser;

        public WindowsSlmgrEvidenceProvider(IProcessRunner runner, ISlmgrXprParser xprParser, ISlmgrDlvParser dlvParser)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _xprParser = xprParser ?? throw new ArgumentNullException(nameof(xprParser));
            _dlvParser = dlvParser ?? throw new ArgumentNullException(nameof(dlvParser));
        }

        public async Task<SlmgrEvidenceResult> CollectAsync(string windowsDirectory, CancellationToken cancellationToken)
        {
            var result = new SlmgrEvidenceResult();
            var cscript = Path.Combine(windowsDirectory ?? string.Empty, "System32", "cscript.exe");
            var script = Path.Combine(windowsDirectory ?? string.Empty, "System32", "slmgr.vbs");
            if (!File.Exists(cscript) || !File.Exists(script))
            {
                result.Warning = "slmgr or cscript is unavailable.";
                return result;
            }

            var xpr = await RunFixedAsync(cscript, script, "/xpr", cancellationToken).ConfigureAwait(false);
            if (xpr.Success) result.Xpr = _xprParser.Parse(xpr.Output, CultureInfo.CurrentCulture); else result.Warning = xpr.Warning;
            cancellationToken.ThrowIfCancellationRequested();
            var dlv = await RunFixedAsync(cscript, script, "/dlv", cancellationToken).ConfigureAwait(false);
            if (dlv.Success) result.Dlv = _dlvParser.Parse(dlv.Output, CultureInfo.CurrentCulture); else result.Warning = Join(result.Warning, dlv.Warning);
            return result;
        }

        internal async Task<FixedCommandResult> RunFixedAsync(string cscript, string script, string option, CancellationToken cancellationToken)
        {
            if (!string.Equals(option, "/xpr", StringComparison.OrdinalIgnoreCase) && !string.Equals(option, "/dlv", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentOutOfRangeException(nameof(option), "Only /xpr and /dlv are read-only allowed options.");
            var execution = await _runner.RunAsync(new ProcessExecutionRequest
            {
                ExecutablePath = cscript,
                Arguments = "//nologo \"" + script + "\" " + option.ToLowerInvariant(),
                WorkingDirectory = Path.GetDirectoryName(script) ?? string.Empty,
                Timeout = TimeSpan.FromSeconds(20),
                CreateNoWindow = true
            }, cancellationToken).ConfigureAwait(false);
            if (execution.WasCancelled) throw new OperationCanceledException(cancellationToken);
            if (execution.TimedOut) return FixedCommandResult.Failed("slmgr timed out.");
            if (execution.StartFailure) return FixedCommandResult.Failed("slmgr could not start.");
            if (execution.ExitCode != 0) return FixedCommandResult.Failed("slmgr returned a nonzero exit code.");
            return FixedCommandResult.Succeeded(execution.StandardOutput);
        }

        private static string Join(string first, string second) => string.IsNullOrEmpty(first) ? second : string.IsNullOrEmpty(second) ? first : first + " " + second;
    }

    public sealed class SlmgrEvidenceResult
    {
        public SlmgrXprParseResult Xpr { get; set; } = new SlmgrXprParseResult();
        public SlmgrDlvParseResult Dlv { get; set; } = new SlmgrDlvParseResult();
        public string Warning { get; set; } = string.Empty;
    }

    public sealed class FixedCommandResult
    {
        public bool Success { get; private set; }
        public string Output { get; private set; } = string.Empty;
        public string Warning { get; private set; } = string.Empty;
        public static FixedCommandResult Succeeded(string output) => new FixedCommandResult { Success = true, Output = output ?? string.Empty };
        public static FixedCommandResult Failed(string warning) => new FixedCommandResult { Warning = warning ?? string.Empty };
    }
}
