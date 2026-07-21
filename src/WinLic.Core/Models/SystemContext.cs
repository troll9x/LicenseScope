namespace WinLic.Core.Models
{
    /// <summary>Contains the machine facts passed to scanners.</summary>
    public sealed class SystemContext
    {
        public string MachineName { get; set; } = string.Empty;
        public string OsName { get; set; } = string.Empty;
        public string OsVersion { get; set; } = string.Empty;
        public string OsBuild { get; set; } = string.Empty;
        public OperatingSystemArchitecture OsArchitecture { get; set; }
        public ProcessArchitecture ProcessArchitecture { get; set; }
        public bool Is64BitOperatingSystem { get; set; }
        public bool Is64BitProcess { get; set; }
        public bool IsAdministrator { get; set; }
        public string WindowsDirectory { get; set; } = string.Empty;
        public string ProgramFilesPath { get; set; } = string.Empty;
        public string ProgramFilesX86Path { get; set; } = string.Empty;
    }
}
