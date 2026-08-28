

namespace Finstance.Models;


public record StatementResult(string BankType, DateOnly CutOffDate, List<ExpenseModel> Expenses)
{
}