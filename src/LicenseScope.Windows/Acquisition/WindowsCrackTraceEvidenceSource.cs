using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using LicenseScope.Core.Contracts;
using LicenseScope.Core.Models;
using LicenseScope.Core.Runtime;
using LicenseScope.Windows.Classification;
using LicenseScope.Windows.Models;
using LicenseScope.Windows.Parsing;

namespace LicenseScope.Windows.Acquisition
{
    public sealed class WindowsCrackTraceEvidenceSource : ICrackTraceEvidenceSource
    {
        internal static readonly string[] ToolKeywords =
        {
            "KMSpico", "KMService", "WinKSO", "KMSELDI", "KMS_VL_ALL",
            "KMSAuto", "AutoKMS", "KMSSS", "KMSEmulator", "vlmcsd",
            "Activation-Renewal", "AAct", "gatherosstate", "clipup", "KMS38"
        };

        internal static readonly string[] PublicKmsKeywords =
        {
            "msguides", "kms.loli", "digiboy.ir", "0t.ng", "kms.chinancce",
            "kmscloud", "kms.cangshui", "kms.ddns.net", "e8.us.to",
            "kms.mrxinwang", "kms8.msguides", "kms9.msguides",
            "kms.xspace.in", "skms.netnr"
        };

        private const string SoftwareProtectionPolicyPath =
            @"SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform";

        private readonly IProcessRunner _processRunner;
        private readonly WindowsEvidenceCollector _licenseEvidence;

        public WindowsCrackTraceEvidenceSource(IProcessRunner processRunner)
        {
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            var slmgr = new WindowsSlmgrEvidenceProvider(
                processRunner,
                new SlmgrXprParser(),
                new SlmgrDlvParser());
            _licenseEvidence = new WindowsEvidenceCollector(
                new WindowsWmiQueryService(),
                new WindowsRegistryReader(),
                slmgr);
        }

        public async Task<CrackTraceEvidenceSnapshot> CollectAsync(
            SystemContext context,
            CancellationToken cancellationToken)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var unavailable = new List<string>();
            var activation = await CollectActivationAsync(context, unavailable, cancellationToken)
                .ConfigureAwait(false);
            var services = CollectServices(unavailable, cancellationToken);
            var processes = CollectProcesses(unavailable, cancellationToken);
            var paths = CollectPaths(context, unavailable, cancellationToken);
            var tasks = await CollectTasksAsync(context, unavailable, cancellationToken)
                .ConfigureAwait(false);
            var events = CollectEvents(unavailable, cancellationToken);
            var registry = CollectRegistry(unavailable);
            return new CrackTraceEvidenceSnapshot
            {
                Activation = activation,
                Services = services,
                Processes = processes,
                Paths = paths,
                Tasks = tasks,
                Events = events,
                RegistryValues = registry,
                UnavailableSources = unavailable.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            };
        }

        private async Task<WindowsActivationTrace> CollectActivationAsync(
            SystemContext context,
            ICollection<string> unavailable,
            CancellationToken token)
        {
            try
            {
                var evidence = await _licenseEvidence.CollectAsync(context, token).ConfigureAwait(false);
                foreach (var warning in evidence.Warnings)
                {
                    if (warning.IndexOf("unavailable", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        warning.IndexOf("denied", StringComparison.OrdinalIgnoreCase) >= 0)
                        unavailable.Add("Activation: " + warning);
                }
                var selected = new WindowsProductSelector().Select(evidence.Products).Product;
                var channel = new WindowsChannelClassifier().Classify(
                    selected?.Description ?? evidence.Dlv.Description,
                    selected?.ProductKeyChannel ?? string.Empty);
                var partial = selected?.PartialProductKey ?? evidence.Dlv.PartialProductKey;
                return new WindowsActivationTrace
                {
                    ProductName = selected?.Name ?? context.OsName,
                    EditionDescription = selected?.Description ?? evidence.Dlv.Description,
                    Channel = channel,
                    ActivationId = selected?.ActivationId ?? string.Empty,
                    PartialProductKey = MaskPartialKey(partial),
                    LicenseStatus = selected?.LicenseStatus,
                    GracePeriodRemaining = selected?.GracePeriodRemaining,
                    ExpirationDate = evidence.Xpr.ExpirationDate ??
                                     evidence.Dlv.ExpirationDate ??
                                     selected?.EvaluationEndDate,
                    IsPermanent = evidence.Xpr.IsPermanent,
                    IndicatesUnlicensed = evidence.Xpr.IndicatesUnlicensed,
                    OemFirmwareKeyPresent = !string.IsNullOrWhiteSpace(evidence.Oa3ProductKey),
                    KmsHost = selected?.KmsMachineName ?? evidence.Dlv.KmsMachineName,
                    KmsPort = selected?.KmsPort
                };
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                unavailable.Add("Activation: " + ex.GetType().Name);
                return new WindowsActivationTrace();
            }
        }

        private static IReadOnlyList<CrackTraceArtifact> CollectServices(
            ICollection<string> unavailable,
            CancellationToken token)
        {
            var result = new List<CrackTraceArtifact>();
            try
            {
                var rows = new WindowsWmiQueryService().Query(
                    "SELECT Name, DisplayName FROM Win32_Service");
                foreach (var row in rows)
                {
                    token.ThrowIfCancellationRequested();
                    var name = GetString(row, "Name");
                    var display = GetString(row, "DisplayName");
                    var keyword = FindKeyword(name + " " + display);
                    if (keyword.Length == 0) continue;
                    result.Add(new CrackTraceArtifact
                    {
                        Source = "Service",
                        Name = string.IsNullOrWhiteSpace(display) ? name : display,
                        MatchedKeyword = keyword,
                        NameMatched = true
                    });
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { unavailable.Add("Service: " + ex.GetType().Name); }
            return result;
        }

        private static IReadOnlyList<CrackTraceArtifact> CollectProcesses(
            ICollection<string> unavailable,
            CancellationToken token)
        {
            var result = new List<CrackTraceArtifact>();
            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    using (process)
                    {
                        token.ThrowIfCancellationRequested();
                        string name;
                        try { name = process.ProcessName; }
                        catch (Exception ex) when (
                            ex is InvalidOperationException ||
                            ex is System.ComponentModel.Win32Exception) { continue; }
                        var keyword = FindKeyword(name);
                        if (keyword.Length == 0) continue;
                        result.Add(new CrackTraceArtifact
                        {
                            Source = "Process",
                            Name = name,
                            MatchedKeyword = keyword,
                            NameMatched = true
                        });
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { unavailable.Add("Process: " + ex.GetType().Name); }
            return result;
        }

        private static IReadOnlyList<CrackTraceArtifact> CollectPaths(
            SystemContext context,
            ICollection<string> unavailable,
            CancellationToken token)
        {
            var result = new List<CrackTraceArtifact>();
            try
            {
                foreach (var candidate in KnownPaths(context))
                {
                    token.ThrowIfCancellationRequested();
                    if (!File.Exists(candidate.Item1) && !Directory.Exists(candidate.Item1)) continue;
                    result.Add(new CrackTraceArtifact
                    {
                        Source = "Path",
                        Name = candidate.Item2,
                        Path = candidate.Item1,
                        MatchedKeyword = candidate.Item2,
                        NameMatched = true
                    });
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { unavailable.Add("Path: " + ex.GetType().Name); }
            return result;
        }

        private async Task<IReadOnlyList<CrackTraceArtifact>> CollectTasksAsync(
            SystemContext context,
            ICollection<string> unavailable,
            CancellationToken token)
        {
            var executable = Path.Combine(context.WindowsDirectory, "System32", "schtasks.exe");
            try
            {
                var execution = await _processRunner.RunAsync(new ProcessExecutionRequest
                {
                    ExecutablePath = executable,
                    Arguments = "/query /fo csv /v /nh",
                    Timeout = TimeSpan.FromSeconds(20),
                    CreateNoWindow = true
                }, token).ConfigureAwait(false);
                if (execution.WasCancelled) throw new OperationCanceledException(token);
                if (execution.StartFailure || execution.TimedOut || execution.ExitCode != 0)
                {
                    unavailable.Add("ScheduledTask: query unavailable");
                    return Array.Empty<CrackTraceArtifact>();
                }
                return ParseTaskCsv(execution.StandardOutput);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                unavailable.Add("ScheduledTask: " + ex.GetType().Name);
                return Array.Empty<CrackTraceArtifact>();
            }
        }

        internal static IReadOnlyList<CrackTraceArtifact> ParseTaskCsv(string output)
        {
            var result = new List<CrackTraceArtifact>();
            foreach (var line in (output ?? string.Empty)
                         .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var fields = ParseCsvLine(line);
                if (fields.Count == 0) continue;
                var taskName = fields.FirstOrDefault(x => x.StartsWith("\\", StringComparison.Ordinal)) ??
                               fields.FirstOrDefault() ?? string.Empty;
                var action = fields.Count > 8 ? fields[8] : string.Empty;
                var nameKeyword = FindKeyword(taskName);
                var actionKeyword = FindKeyword(action);
                if (nameKeyword.Length == 0 && actionKeyword.Length == 0) continue;
                result.Add(new CrackTraceArtifact
                {
                    Source = "ScheduledTask",
                    Name = Path.GetFileName(taskName.TrimEnd('\\')),
                    Path = taskName,
                    Action = SafeAction(action),
                    MatchedKeyword = actionKeyword.Length > 0 ? actionKeyword : nameKeyword,
                    NameMatched = nameKeyword.Length > 0,
                    ActionMatched = actionKeyword.Length > 0
                });
            }
            return result;
        }

        private static IReadOnlyList<CrackTraceArtifact> CollectEvents(
            ICollection<string> unavailable,
            CancellationToken token)
        {
            var result = new List<CrackTraceArtifact>();
            try
            {
                using (var log = new EventLog("Application"))
                {
                    var inspected = 0;
                    for (var index = log.Entries.Count - 1; index >= 0 && inspected < 256; index--, inspected++)
                    {
                        token.ThrowIfCancellationRequested();
                        var entry = log.Entries[index];
                        if (entry.Source.IndexOf("Software Protection", StringComparison.OrdinalIgnoreCase) < 0 &&
                            entry.Source.IndexOf("spp", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                        var keyword = FindKeyword(entry.Message);
                        if (keyword.Length == 0)
                            keyword = PublicKmsKeywords.FirstOrDefault(x =>
                                entry.Message.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0) ??
                                      string.Empty;
                        if (keyword.Length == 0) continue;
                        result.Add(new CrackTraceArtifact
                        {
                            Source = "EventLog",
                            Name = entry.Source + " / Event " + entry.InstanceId,
                            MatchedKeyword = keyword,
                            NameMatched = true
                        });
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { unavailable.Add("EventLog: " + ex.GetType().Name); }
            return result;
        }

        private static IReadOnlyList<CrackTraceRegistryEvidence> CollectRegistry(
            ICollection<string> unavailable)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(SoftwareProtectionPolicyPath, false))
                {
                    var value = key?.GetValue("NoGenTicket");
                    return new[]
                    {
                        new CrackTraceRegistryEvidence
                        {
                            Path = @"HKLM\" + SoftwareProtectionPolicyPath,
                            Name = "NoGenTicket",
                            Value = value == null ? string.Empty : Convert.ToString(value) ?? string.Empty,
                            Present = value != null
                        }
                    };
                }
            }
            catch (Exception ex) when (
                ex is UnauthorizedAccessException ||
                ex is System.Security.SecurityException ||
                ex is IOException)
            {
                unavailable.Add("Registry: " + ex.GetType().Name);
                return Array.Empty<CrackTraceRegistryEvidence>();
            }
        }

        private static IEnumerable<Tuple<string, string>> KnownPaths(SystemContext context)
        {
            var programFiles = context.ProgramFilesPath;
            var programFilesX86 = context.ProgramFilesX86Path;
            var system = Path.Combine(context.WindowsDirectory, "System32");
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return new[]
            {
                Tuple.Create(Path.Combine(programFiles, "KMSpico"), "KMSpico"),
                Tuple.Create(Path.Combine(programFilesX86, "KMSpico"), "KMSpico"),
                Tuple.Create(Path.Combine(programData, "KMSpico"), "KMSpico"),
                Tuple.Create(Path.Combine(system, "KMSELDI.exe"), "KMSELDI"),
                Tuple.Create(Path.Combine(programFiles, "KMSAuto Net"), "KMSAuto"),
                Tuple.Create(Path.Combine(programFilesX86, "KMSAuto Net"), "KMSAuto"),
                Tuple.Create(Path.Combine(programFiles, "KMSAuto"), "KMSAuto"),
                Tuple.Create(Path.Combine(programFilesX86, "KMSAuto"), "KMSAuto"),
                Tuple.Create(
                    Path.Combine(programData, @"Microsoft\Windows\ClipSVC\GenuineTicket\GenuineTicket.xml"),
                    "GenuineTicket"),
                Tuple.Create(
                    Path.Combine(programFiles, @"Activation-Renewal\Activation_task.cmd"),
                    "Activation-Renewal"),
                Tuple.Create(
                    Path.Combine(programFiles, @"Activation-Renewal\Info.txt"),
                    "Activation-Renewal")
            };
        }

        private static string GetString(IReadOnlyDictionary<string, object?> row, string key)
        {
            object? value;
            return row.TryGetValue(key, out value) && value != null
                ? Convert.ToString(value) ?? string.Empty
                : string.Empty;
        }

        private static string FindKeyword(string value)
        {
            return ToolKeywords.FirstOrDefault(x =>
                (value ?? string.Empty).IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0) ??
                   string.Empty;
        }

        private static string MaskPartialKey(string value)
        {
            var compact = (value ?? string.Empty).Replace("-", string.Empty).Trim();
            if (compact.Length == 0) return string.Empty;
            var suffix = compact.Length <= 5 ? compact : compact.Substring(compact.Length - 5);
            return "XXXXX-XXXXX-XXXXX-XXXXX-" + suffix.ToUpperInvariant();
        }

        private static string SafeAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action)) return string.Empty;
            var expanded = Environment.ExpandEnvironmentVariables(action.Trim());
            var fileName = Path.GetFileName(expanded.Trim('"'));
            return string.IsNullOrWhiteSpace(fileName) ? "[action present]" : fileName;
        }

        private static IReadOnlyList<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new System.Text.StringBuilder();
            var quoted = false;
            var input = line ?? string.Empty;
            for (var index = 0; index < input.Length; index++)
            {
                var character = input[index];
                if (character == '"')
                {
                    if (quoted && index + 1 < input.Length && input[index + 1] == '"')
                    {
                        current.Append('"');
                        index++;
                    }
                    else quoted = !quoted;
                }
                else if (character == ',' && !quoted)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else current.Append(character);
            }
            fields.Add(current.ToString());
            return fields;
        }
    }
}
