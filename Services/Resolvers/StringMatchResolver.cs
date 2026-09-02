using System.Text.Json;
using Finstance.dbContext.Models;
using Finstance.Services.Resolvers;

namespace Finstance.Services.Resolvers;


public class StringMatchResolver : ICategoryResolver
{
    public int Priority {get;set;}
    private readonly Dictionary<ExpenseCategory, List<string>> _categoryKeywords;

    public StringMatchResolver(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var raw = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
            ?? throw new InvalidOperationException("categoryKeywords.json okunamadı.");

        _categoryKeywords = new Dictionary<ExpenseCategory, List<string>>();

        foreach (var (categoryName, keywords) in raw)
        {
            if (Enum.TryParse<ExpenseCategory>(categoryName, ignoreCase: true, out var category))
            {
                _categoryKeywords[category] = keywords
                    .Select(k => k.ToUpperInvariant())
                    .ToList();
            }
        }
    }

    public ExpenseCategory? Resolve(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
            return ExpenseCategory.Diger;

        var normalized = string.Concat(locationName.ToUpperInvariant().Select(x =>
 {
     switch (x)
     {
         case 'Ö': x = 'O'; break;
         case 'İ': x = 'I'; break;
         case 'Ü': x = 'U'; break;
         case 'Ş': x = 'S'; break;
         case 'Ç': x = 'C'; break;
         case 'Ğ': x = 'G'; break;
     }
     return x;
 }));

        foreach (var (category, keywords) in _categoryKeywords)
        {
            foreach (var keyword in keywords)
            {
                if (normalized.Contains(keyword))
                    return category;
            }
        }

        return ExpenseCategory.Diger;
    }
}
