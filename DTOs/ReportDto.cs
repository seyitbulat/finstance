namespace Finstance.DTOs;


public class ReportDto
{
    public DateOnly RequestDate { get; set; }

    public List<ReportDetailDto> Details { get; set; }
}


public class ReportDetailDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }

    public int LocationId { get; set; }
    public int UserId { get; set; }
    public int BankStatementId { get; set; }

    public string LocationName { get; set; }
    public DateOnly CutOffDate { get; set; }

}