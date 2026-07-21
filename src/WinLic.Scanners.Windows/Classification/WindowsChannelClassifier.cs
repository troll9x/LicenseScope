using System;

namespace WinLic.Scanners.Windows.Classification
{
    public sealed class WindowsChannelClassifier
    {
        public string Classify(string description, string productKeyChannel)
        {
            var value = ((description ?? string.Empty) + " " + (productKeyChannel ?? string.Empty)).ToUpperInvariant();
            if (value.Contains("VOLUME_KMSCLIENT")) return "Volume_KMSCLIENT";
            if (value.Contains("VOLUME_MAK")) return "Volume_MAK";
            if (value.Contains("VOLUME_KMS")) return "Volume_KMS";
            if (value.Contains("OEM_DM")) return "OEM_DM";
            if (value.Contains("OEM_SLP")) return "OEM_SLP";
            if (value.Contains("OEM_COA")) return "OEM_COA";
            if (value.Contains("EVALUATION") || value.Contains("TIMEBASED")) return "Evaluation";
            if (value.Contains("RETAIL")) return "Retail";
            return "Unknown";
        }
    }
}
