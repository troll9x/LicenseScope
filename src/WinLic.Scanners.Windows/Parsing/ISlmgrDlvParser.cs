using System.Globalization;
using WinLic.Scanners.Windows.Models;

namespace WinLic.Scanners.Windows.Parsing
{
    public interface ISlmgrDlvParser { SlmgrDlvParseResult Parse(string output, CultureInfo culture); }
}
