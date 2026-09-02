using Finstance.dbContext.Models;
using Finstance.Services.Resolvers;

namespace Finstance.Services;



public class CategoryPipeline
{
    private StringMatchResolver _stringMatchResolver { get; set; }

    public CategoryPipeline(StringMatchResolver stringMatchResolver)
    {
        _stringMatchResolver = stringMatchResolver;
    }

    
    public ExpenseCategory Process(string locationName)
    {
        var resolve = _stringMatchResolver.Resolve(locationName);

        if(resolve == null)
        {
            return ExpenseCategory.Diger;
        }

        return resolve.Value;
    }
}