namespace Finstance.Models;


public class ExpenseModel
{
    public DateOnly Date { get; set; }
    public string Location { get; set; }
    public decimal Amount { get; set; }


    public ExpenseModel(DateOnly date, string location, decimal amount)
    {
        Date = date;
        Location = location;
        Amount = amount;
    }
}