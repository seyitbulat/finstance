using Finstance.dbContext;
using Finstance.dbContext.Models;
using Finstance.DTOs;
using Finstance.Models;
using Finstance.Services.Helpers;
using Finstance.Services.Resolvers;
using Microsoft.EntityFrameworkCore;

namespace Finstance.Services;


public class DataService
{
    private readonly DataBaseContext _dbContext;
    private readonly LocationPipeline _locationPipeline;
    public DataService(DataBaseContext dbContext, LocationPipeline locationPipeline)
    {
        _dbContext = dbContext;
        _locationPipeline = locationPipeline;
    }

    public async Task<bool> IsStatementExistsAsync(DateOnly cutOffDate)
    {
        return await _dbContext.BankStatements
            .AnyAsync(s => s.CutOffDate == cutOffDate);
    }

    public async Task SaveAsync(StatementResult data)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var bankStatement = new BankStatementModel
            {
                UserId = 1,
                CutOffDate = data.CutOffDate
            };
            _dbContext.BankStatements.Add(bankStatement);
            await _dbContext.SaveChangesAsync();

            foreach (var expense in data.Expenses)
            {
                var location = await _locationPipeline.ProcessAsync(expense.Location);

                if (location == null)
                {
                    location = new()
                    {
                        Name = expense.Location,
                        NormalizedName = NormalizerHelper.NormalizeTurkish(expense.Location),
                        Category = ExpenseCategory.Diger
                    };

                    var newLoc = await _dbContext.Locations.AddAsync(location);
                    await _dbContext.SaveChangesAsync();

                    var dbExpense = new dbContext.Models.ExpenseModel
                    {
                        Date = expense.Date,
                        Amount = expense.Amount,
                        LocationId = newLoc.Entity.Id,
                        UserId = 1,
                        BankStatementId = bankStatement.Id,
                        IsInstalment = expense.IsInstalment
                    };
                    _dbContext.Expenses.Add(dbExpense);
                }
                else
                {
                    var dbExpense = new dbContext.Models.ExpenseModel
                    {
                        Date = expense.Date,
                        Amount = expense.Amount,
                        LocationId = location.Id,
                        UserId = 1,
                        BankStatementId = bankStatement.Id,
                        IsInstalment = expense.IsInstalment
                    };
                    _dbContext.Expenses.Add(dbExpense);
                }
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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


   public void SeedData()
{
    var locations = ExpenseLocationSeedData.Locations;

    if (!_dbContext.Locations.Any())
    {
        _dbContext.AddRange(locations);
        _dbContext.SaveChanges();

        _dbContext.Database.ExecuteSqlRaw(
            @"SELECT setval(pg_get_serial_sequence('""Locations""', 'Id'), (SELECT MAX(""Id"") FROM ""Locations""));"
        );
    }
}
}