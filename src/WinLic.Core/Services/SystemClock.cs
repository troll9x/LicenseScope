using System;
using WinLic.Core.Contracts;

namespace WinLic.Core.Services
{
    /// <summary>Production UTC clock.</summary>
    public sealed class SystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
