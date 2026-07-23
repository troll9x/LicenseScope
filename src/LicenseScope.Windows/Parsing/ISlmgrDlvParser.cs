using System.Globalization;
using LicenseScope.Windows.Models;

namespace LicenseScope.Windows.Parsing
{
    public interface ISlmgrDlvParser { SlmgrDlvParseResult Parse(string output, CultureInfo culture); }
}
