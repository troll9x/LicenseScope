using WinLic.Core.Models;

namespace WinLic.Core.Contracts
{
    /// <summary>Provides facts about the current operating environment.</summary>
    public interface ISystemContextProvider { SystemContext GetCurrent(); }
}
