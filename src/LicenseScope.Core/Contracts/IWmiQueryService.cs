using System.Collections.Generic;

namespace LicenseScope.Core.Contracts
{
    /// <summary>Read-only abstraction for future WMI evidence providers.</summary>
    public interface IWmiQueryService
    {
        IReadOnlyList<IReadOnlyDictionary<string, object?>> Query(string wql);
    }
}
