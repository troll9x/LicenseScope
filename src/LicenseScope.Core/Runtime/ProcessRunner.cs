using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LicenseScope.Core.Contracts;

namespace LicenseScope.Core.Runtime
{
    /// <summary>Runs executables directly with redirected output, cancellation, and timeout handling.</summary>
    public sealed class ProcessRunner : IProcessRunner
    {
        public async Task<ProcessExecutionResult> RunAsync(ProcessExecutionRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.ExecutablePath)) throw new ArgumentException("ExecutablePath is required.", nameof(request));
            if (request.Timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(request), "Timeout must be positive.");

            var stopwatch = Stopwatch.StartNew();
            var result = new ProcessExecutionResult();
            if (!File.Exists(request.ExecutablePath))
            {
                result.StartFailure = true;
                result.StartFailureMessage = "Executable does not exist.";
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            using (var process = new Process())
            using (var cancellation = new CancellationTokenSource())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = request.ExecutablePath,
                    Arguments = request.Arguments ?? string.Empty,
                    WorkingDirectory = request.WorkingDirectory ?? string.Empty,
                    UseShellExecute = false,
                    RedirectStandardOutput = request.RedirectStandardOutput,
                    RedirectStandardError = request.RedirectStandardError,
                    CreateNoWindow = request.CreateNoWindow
                };
                process.EnableRaisingEvents = true;
                var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                process.Exited += (_, __) => exited.TrySetResult(true);

                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.WasCancelled = true;
                        return result;
                    }
                    process.Start();
                    var stdout = request.RedirectStandardOutput ? process.StandardOutput.ReadToEndAsync() : Task.FromResult(string.Empty);
                    var stderr = request.RedirectStandardError ? process.StandardError.ReadToEndAsync() : Task.FromResult(string.Empty);
                    if (process.HasExited) exited.TrySetResult(true);

                    var timeoutTask = Task.Delay(request.Timeout, cancellation.Token);
                    var cancelledTask = Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    var completed = await Task.WhenAny(exited.Task, timeoutTask, cancelledTask).ConfigureAwait(false);
                    if (completed == cancelledTask) result.WasCancelled = true;
                    else if (completed == timeoutTask) result.TimedOut = true;
                    else cancellation.Cancel();

                    if ((result.WasCancelled || result.TimedOut) && !process.HasExited)
                    {
                        if (!TryKill(process)) return result;
                    }
                    await exited.Task.ConfigureAwait(false);
                    result.StandardOutput = await stdout.ConfigureAwait(false);
                    result.StandardError = await stderr.ConfigureAwait(false);
                    result.ExitCode = process.ExitCode;
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
                {
                    result.StartFailure = true;
                    result.StartFailureMessage = ex.Message;
                }
                finally
                {
                    result.Duration = stopwatch.Elapsed;
                }
                return result;
            }
        }

        private static bool TryKill(Process process)
        {
            try
            {
                process.Kill();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }
    }
}
