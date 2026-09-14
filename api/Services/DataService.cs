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
    private readonly CategoryPipeline _categoryPipeline;
    public DataService(DataBaseContext dbContext, LocationPipeline locationPipeline, CategoryPipeline categoryPipeline)
    {
        _dbContext = dbContext;
        _locationPipeline = locationPipeline;
        _categoryPipeline = categoryPipeline;
    }

    public async Task<bool> IsStatementExistsAsync(DateOnly cutOffDate, int userId)
    {
        return await _dbContext.BankStatements
            .AnyAsync(s => s.CutOffDate == cutOffDate && s.UserId == userId);
    }

    public async Task SaveAsync(StatementResult data, int userId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var bankStatement = new BankStatementModel
            {
                UserId = userId,
                CutOffDate = data.CutOffDate
            };
            _dbContext.BankStatements.Add(bankStatement);
            await _dbContext.SaveChangesAsync();

            foreach (var expense in data.Expenses)
            {
                var location = await _locationPipeline.ProcessAsync(expense.Location);

                if (location == null)
                {
                    var category = await _categoryPipeline.ProcessAsync(expense.Location)
                                   ?? ExpenseCategory.Diger;

                    location = new()
                    {
                        Name = expense.Location,
                        NormalizedName = NormalizerHelper.NormalizeTurkish(expense.Location),
                        Category = category
                    };

                    var newLoc = await _dbContext.Locations.AddAsync(location);
                    await _dbContext.SaveChangesAsync();
                    location = newLoc.Entity;
                }

                _dbContext.Expenses.Add(new dbContext.Models.ExpenseModel
                {
                    Date = expense.Date,
                    Amount = expense.Amount,
                    LocationId = location.Id,
                    UserId = userId,
                    BankStatementId = bankStatement.Id,
                    IsInstalment = expense.IsInstalment
                });
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

    public ReportDto GetMonthlyReport(DateOnly requestDate, int userId)
    {
        var expenses = _dbContext.Expenses
            .Include(x => x.BankStatement)
            .Include(x => x.Location)
            .Where(x => x.UserId == userId && 
                        x.BankStatement.CutOffDate.Month == requestDate.Month
                     && x.BankStatement.CutOffDate.Year == requestDate.Year)
            .ToList()
            .Select(x => new ReportDetailDto
            {
                Id = x.Id,
                Amount = x.Amount,
                BankStatementId = x.BankStatement.Id,
                CutOffDate = x.BankStatement.CutOffDate,
                Date = x.Date,
                LocationId = x.Location.Id,
                LocationName = x.Location.Name,
                Category = x.Location.Category.ToString()
            })
            .ToList();

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