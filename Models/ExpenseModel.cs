namespace Finstance.Models;


public class ExpenseModel
{
    public DateOnly Date { get; set; }
    public string Location { get; set; }
    public decimal Amount { get; set; }
    public bool IsInstalment { get; set; }


    public ExpenseModel(DateOnly date, string location, decimal amount, bool isInstalment)
    {
        Date = date;
        Location = location;
        Amount = amount;
        IsInstalment = isInstalment;
    }
}