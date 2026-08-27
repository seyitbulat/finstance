

namespace Finstance.Models;


public record StatementResult(DateOnly CutOffDate, List<ExpenseModel> Expenses)
{
}