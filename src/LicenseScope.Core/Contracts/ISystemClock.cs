using System;

namespace LicenseScope.Core.Contracts
{
    /// <summary>Provides a replaceable UTC clock.</summary>
    public interface ISystemClock { DateTimeOffset UtcNow { get; } }
}
