
using Finstance.dbContext.Models;

namespace Finstance.Services.Resolvers;



public interface ICategoryResolver
{

    ExpenseCategory? Resolve(string locationName);
    int Priority  { get; set; }
}