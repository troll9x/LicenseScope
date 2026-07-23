using LicenseScope.Core.Models;

namespace LicenseScope.Core.Contracts
{
    /// <summary>Provides facts about the current operating environment.</summary>
    public interface ISystemContextProvider { SystemContext GetCurrent(); }
}
