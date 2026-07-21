using System;
using System.Collections.Generic;

namespace WinLic.Scanners.Windows.Classification
{
    public sealed class WindowsKnownKeyCatalog : IWindowsKnownKeyCatalog
    {
        // Public Microsoft generic installation/GVLK last-five suffixes used only as supporting evidence.
        private static readonly HashSet<string> Generic = new HashSet<string>(new[] { "3V66T", "8HVX7", "H8Q99", "WXCHW", "WGGBY", "2YV77", "8DEC2", "VCFB2" }, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> Volume = new HashSet<string>(new[] { "T83GX", "GCQG9", "6Q84J", "6XYWF", "J447Y", "66QFC", "2YT43", "KHJW4", "J462D", "7CG2H", "9D6T9", "MKKG7" }, StringComparer.OrdinalIgnoreCase);
        public bool IsGenericInstallationKey(string partialKey) => Generic.Contains(partialKey ?? string.Empty);
        public bool IsVolumeClientKey(string partialKey) => Volume.Contains(partialKey ?? string.Empty);
        public string GetDescription(string partialKey) => IsGenericInstallationKey(partialKey) ? "Microsoft generic installation key suffix" : IsVolumeClientKey(partialKey) ? "Microsoft volume client key suffix" : string.Empty;
    }
}
