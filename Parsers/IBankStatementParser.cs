
using System.Globalization;
using Finstance.Models;
using UglyToad.PdfPig;

namespace Finstance.Parsers;


public interface IBankStatementParser
{
    bool CanHandle(PdfDocument doc);

    DateOnly ExtractCutOffDate(PdfDocument doc);

    CultureInfo GetCulture();

    List<ExpenseModel> ParseExpenses(PdfDocument doc);

}