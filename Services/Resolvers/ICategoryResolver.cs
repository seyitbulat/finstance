
using Finstance.dbContext.Models;

namespace Finstance.Services.Resolvers;



public interface ICategoryResolver
{

    Task<ExpenseCategory?> ResolveAsync(string locationName);
    int Priority  { get; }
}