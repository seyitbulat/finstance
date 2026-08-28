

using Finstance.Models;
using Finstance.Parsers;
using UglyToad.PdfPig;

namespace Finstance.Services;


public class StatementService
{
    private readonly IEnumerable<IBankStatementParser> _parsers;


    public StatementService(IEnumerable<IBankStatementParser> parsers)
    {
        _parsers = parsers;
    }


    public StatementResult Process(PdfDocument doc)
    {

        var parser = _parsers.FirstOrDefault(p => p.CanHandle(doc));

        if (parser == null)
            throw new Exception("Unknown Bank");


        var cutOffDate = parser.ExtractCutOffDate(doc);

        var expenses = parser.ParseExpenses(doc);

        var bankType = parser.GetBankType();
        return new StatementResult(bankType,cutOffDate, expenses);

    }

}