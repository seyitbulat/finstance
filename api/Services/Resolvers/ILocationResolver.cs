
using Finstance.dbContext.Models;

namespace Finstance.Services.Resolvers;



public interface ILocationResolver
{

    Task<ExpenseLocationModel?> ResolveAsync(string locationName);
    int Priority  { get; }
}