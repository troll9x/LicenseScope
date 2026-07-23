using System.Globalization;
using LicenseScope.Windows.Models;

namespace LicenseScope.Windows.Parsing
{
    public interface ISlmgrXprParser { SlmgrXprParseResult Parse(string output, CultureInfo culture); }
}
