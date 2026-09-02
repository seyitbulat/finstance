using Finstance.dbContext.Models;
using Finstance.Services.Resolvers;

namespace Finstance.Services;



public class CategoryPipeline
{
    private readonly IEnumerable<ICategoryResolver> _resolvers;
    
    public CategoryPipeline(IEnumerable<ICategoryResolver> resolvers)
    {
        _resolvers = resolvers;
    }

    
    public ExpenseCategory Process(string locationName)
    {
        var resolvers = _resolvers.OrderBy(x => x.Priority);

        foreach(var resolver in resolvers)
        {
            var res = resolver.Resolve(locationName);

            if(res != null)
            {
                return res.Value;
            }
        }

        return ExpenseCategory.Diger;
    }
}