namespace Finstance.dbContext.Models;



public class ExpenseModel
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }

    public int LocationId { get; set; }
    public int UserId { get; set; }
    public int BankStatementId { get; set; }
    
    public bool IsInstalment {get; set;}

    public ExpenseLocationModel Location { get; set; }
    public UserModel User { get; set; }
    public BankStatementModel BankStatement { get; set; }

}