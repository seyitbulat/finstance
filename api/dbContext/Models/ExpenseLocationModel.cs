namespace Finstance.dbContext.Models;

public class ExpenseLocationModel
{
    public int Id { get; set; }
    public string Name { get; set; }

    public string NormalizedName { get; set; }
    public ExpenseCategory Category { get; set; }
}