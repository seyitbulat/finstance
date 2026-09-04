using System.Text.Json;
using Finstance.dbContext;
using Finstance.dbContext.Models;
using Finstance.Services.Helpers;
using Finstance.Services.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Finstance.Services.Resolvers;


public class StringMatchResolver : ILocationResolver
{
    private readonly DataBaseContext _dbContext;
    public int Priority { get; } = 0;

    public StringMatchResolver(DataBaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExpenseLocationModel?> ResolveAsync(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
            return null;
        var normalized = NormalizerHelper.NormalizeTurkish(locationName);

        var result = await _dbContext.Locations.Where(l => normalized.Contains(l.NormalizedName)).OrderByDescending(l => l.NormalizedName.Length).FirstOrDefaultAsync();

        return result;
    }
}
