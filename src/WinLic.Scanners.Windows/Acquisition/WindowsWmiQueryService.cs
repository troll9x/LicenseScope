using System;
using System.Collections.Generic;
using System.Management;
using WinLic.Core.Contracts;

namespace WinLic.Scanners.Windows.Acquisition
{
    public sealed class WindowsWmiQueryService : IWmiQueryService
    {
        public IReadOnlyList<IReadOnlyDictionary<string, object?>> Query(string wql)
        {
            if (string.IsNullOrWhiteSpace(wql) || !wql.TrimStart().StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase) || wql.IndexOf(';') >= 0)
                throw new ArgumentException("Only a single WMI SELECT query is allowed.", nameof(wql));

            var rows = new List<IReadOnlyDictionary<string, object?>>();
            using (var searcher = new ManagementObjectSearcher(wql))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject item in results)
                using (item)
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    foreach (PropertyData property in item.Properties) row[property.Name] = property.Value;
                    rows.Add(row);
                }
            }
            return rows;
        }
    }
}
