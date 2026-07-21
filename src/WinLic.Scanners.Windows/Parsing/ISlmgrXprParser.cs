using System.Globalization;
using WinLic.Scanners.Windows.Models;

namespace WinLic.Scanners.Windows.Parsing
{
    public interface ISlmgrXprParser { SlmgrXprParseResult Parse(string output, CultureInfo culture); }
}
