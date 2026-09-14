namespace Finstance.dbContext.Models;



public class BankStatementModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly CutOffDate { get; set; }

    
    public UserModel User { get; set; }

}