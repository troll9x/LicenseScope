namespace LicenseScope.Windows.Acquisition
{
    public sealed class RegistryReadResult
    {
        public bool Found { get; set; }
        public bool AccessDenied { get; set; }
        public string Value { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
