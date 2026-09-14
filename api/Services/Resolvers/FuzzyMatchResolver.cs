using System.Text.Json;
using Finstance.dbContext;
using Finstance.dbContext.Models;
using Finstance.FuzzyMatch;
using Finstance.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Finstance.Services.Resolvers;



public class FuzzyMatchResolver : ILocationResolver
{
    public int Priority { get; } = 1;

    private readonly DataBaseContext _dbContext;
    private const double Treshold = 0.6;

    public FuzzyMatchResolver(DataBaseContext dbContext)
    {
       _dbContext = dbContext;
    }

    public async Task<ExpenseLocationModel?> ResolveAsync(string locationName)
    {
        var normalized = NormalizerHelper.NormalizeTurkish(locationName);

        var result = await _dbContext.Locations.Where(l => EF.Functions.TrigramsSimilarity(normalized, l.NormalizedName) > Treshold).OrderByDescending(l => EF.Functions.TrigramsSimilarity(normalized, l.NormalizedName)).FirstOrDefaultAsync();

        return result;
    }
}