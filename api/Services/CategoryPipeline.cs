using Finstance.dbContext.Models;
using Finstance.Services.Helpers;
using Finstance.Services.Resolvers;

namespace Finstance.Services;



public class CategoryPipeline
{
    private readonly IEnumerable<ICategoryResolver> _resolvers;
    
    public CategoryPipeline(IEnumerable<ICategoryResolver> resolvers)
    {
        _resolvers = resolvers;
    }

    
    public async Task<ExpenseCategory?> ProcessAsync(string locationName)
    {
        var resolvers = _resolvers.OrderBy(x => x.Priority);

        foreach(var resolver in resolvers)
        {
            var res = await resolver.ResolveAsync(locationName);

            if(res != null)
            {
                return res;
            }
        }

        return null;
    }
}