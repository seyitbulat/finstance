using Finstance.dbContext;
using Finstance.dbContext.Models;
using Finstance.DTOs;
using Finstance.Models;
using Finstance.Services.Resolvers;
using Microsoft.EntityFrameworkCore;

namespace Finstance.Services;


public class DataService
{
    private readonly DataBaseContext _dbContext;
    private readonly StringMatchResolver _categoryResolver;
    private readonly CategoryPipeline _categoryPipeline;
    public DataService(DataBaseContext dbContext, CategoryPipeline categoryPipeline)
    {
        _dbContext = dbContext;
        _categoryPipeline = categoryPipeline;
    }

    public async Task<bool> IsStatementExistsAsync(DateOnly cutOffDate)
    {
        return await _dbContext.BankStatements
            .AnyAsync(s => s.CutOffDate == cutOffDate);
    }

    public async Task SaveAsync(StatementResult data)
    {
        var uniqueLocationNames = data.Expenses
            .Select(e => e.Location)
            .Distinct()
            .ToList();

        var existingLocations = await _dbContext.Locations
            .Where(l => uniqueLocationNames.Contains(l.Name))
            .ToDictionaryAsync(l => l.Name, l => l.Id);

        foreach (var name in uniqueLocationNames)
        {
            if (!existingLocations.ContainsKey(name))
            {
                var newLocation = new ExpenseLocationModel
                {
                    Name = name,
                    Category = _categoryPipeline.Process(name)
                };
                _dbContext.Locations.Add(newLocation);
                await _dbContext.SaveChangesAsync();
                existingLocations[name] = newLocation.Id;
            }
        }

        var bankStatement = new BankStatementModel
        {
            UserId = 1,
            CutOffDate = data.CutOffDate
        };
        _dbContext.BankStatements.Add(bankStatement);
        await _dbContext.SaveChangesAsync();

        foreach (var expense in data.Expenses)
        {
            var dbExpense = new dbContext.Models.ExpenseModel
            {
                Date = expense.Date,
                Amount = expense.Amount,
                LocationId = existingLocations[expense.Location],
                UserId = 1,
                BankStatementId = bankStatement.Id,
                IsInstalment = expense.IsInstalment
            };
            _dbContext.Expenses.Add(dbExpense);
        }

        await _dbContext.SaveChangesAsync();
    }

    public ReportDto GetMonthlyReport(DateOnly requestDate)
    {
        var expenses = _dbContext.Expenses.Include(x => x.BankStatement).Include(x => x.Location).Where(x => x.BankStatement.CutOffDate.Month == requestDate.Month).ToList().Select(x =>
        {
            return new ReportDetailDto
            {
                Amount = x.Amount,
                BankStatementId = x.BankStatement.Id,
                CutOffDate = x.BankStatement.CutOffDate,
                Date = x.Date,
                LocationId = x.Location.Id,
                LocationName = x.Location.Name,
                Category = x.Location.Category.ToString()
            };
        }).ToList();


        ReportDto response = new()
        {
            RequestDate = requestDate,
            Details = expenses
        };

        return response;
    }
}