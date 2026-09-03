using Finstance.dbContext.Models;
using Finstance.Services.Helpers;
using Finstance.Services.Resolvers;

namespace Finstance.Services;



public class LocationPipeline
{
    private readonly IEnumerable<ILocationResolver> _resolvers;
    
    public LocationPipeline(IEnumerable<ILocationResolver> resolvers)
    {
        _resolvers = resolvers;
    }

    
    public async Task<ExpenseLocationModel?> ProcessAsync(string locationName)
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