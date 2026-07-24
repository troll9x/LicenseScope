using System;
using LicenseScope.Compatibility;

namespace LicenseScope.Cli
{
    public sealed partial class CliApplication
    {
        private int PrintCompatibility(string[] args)
        {
            if (args.Length > 2 || (args.Length == 2 && args[1] != "--json")) { _console.WriteLine("Cách dùng: LicenseScope.Cli.exe compatibility [--json]"); return 4; }
            var environment = new RuntimeEnvironmentDetector(new WindowsArchitectureProbe()).Detect();
            var payload = CurrentPayload.Describe(environment);
            var assessment = new CompatibilityEvaluator().Evaluate(environment, payload);
            if (args.Length == 2)
                _console.WriteLine("{\"operatingSystem\":\"" + Escape(environment.WindowsProductName) + "\",\"windowsBuild\":\"" + Escape(environment.WindowsBuild) + "\",\"nativeArchitecture\":\"" + environment.NativeOsArchitecture + "\",\"processArchitecture\":\"" + environment.ProcessArchitecture + "\",\"executionMode\":\"" + environment.ExecutionMode + "\",\"netFramework\":\"" + environment.InstalledNetFrameworkVersion + "\",\"payload\":\"" + payload.Id + "\",\"compatibility\":\"" + assessment.SupportLevel + "\",\"runtimeVerified\":" + assessment.RuntimeVerified.ToString().ToLowerInvariant() + ",\"endOfLife\":" + assessment.IsEndOfLife.ToString().ToLowerInvariant() + "}");
            else
            {
                _console.WriteLine("Hệ điều hành: " + environment.WindowsProductName); _console.WriteLine("Bản dựng Windows: " + environment.WindowsBuild);
                _console.WriteLine("Kiến trúc hệ điều hành: " + environment.NativeOsArchitecture); _console.WriteLine("Kiến trúc tiến trình: " + environment.ProcessArchitecture);
                _console.WriteLine("Chế độ thực thi: " + environment.ExecutionMode); _console.WriteLine(".NET Framework: " + environment.InstalledNetFrameworkVersion);
                _console.WriteLine("Gói thực thi: " + payload.Id); _console.WriteLine("Mức tương thích: " + assessment.SupportLevel);
                _console.WriteLine("Runtime verified: " + assessment.RuntimeVerified); _console.WriteLine("Security support: " + (assessment.IsEndOfLife ? "EndOfLife" : "Current"));
                foreach (var warning in assessment.Warnings) _console.WriteLine("Cảnh báo: " + warning);
                foreach (var reason in assessment.BlockingReasons) _console.WriteLine("Lý do chặn: " + reason);
            }
            return assessment.CanStart ? 0 : environment.InstalledNetFrameworkVersion < new Version(4, 8) ? 8 : assessment.SupportLevel == CompatibilitySupportLevel.Unknown ? 9 : 7;
        }
        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
