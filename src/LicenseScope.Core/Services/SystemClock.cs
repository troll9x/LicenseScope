using System;
using LicenseScope.Core.Contracts;

namespace LicenseScope.Core.Services
{
    /// <summary>Production UTC clock.</summary>
    public sealed class SystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
