

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
    
    public (string BankType, DateOnly CutOffDate) ExtractMetadata(PdfDocument doc)
    {
        var parser = ResolveParser(doc);
        return (parser.GetBankType(), parser.ExtractCutOffDate(doc));
    }

    public StatementResult Process(PdfDocument doc)
    {
        var parser = ResolveParser(doc);


        var cutOffDate = parser.ExtractCutOffDate(doc);

        var expenses = parser.ParseExpensesNonTabular(doc);

        var bankType = parser.GetBankType();
        return new StatementResult(bankType, cutOffDate, expenses);

    }


    public IBankStatementParser ResolveParser(PdfDocument doc)
    {
        var parser = _parsers.FirstOrDefault(p => p.CanHandle(doc));

        if (parser == null)
            throw new Exception("Unknown Bank");

        return parser;
    }

}