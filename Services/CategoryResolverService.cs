using System.Text.Json;
using Finstance.dbContext.Models;

namespace Finstance.Services;


public class CategoryResolverService
{
    private readonly Dictionary<ExpenseCategory, List<string>> _categoryKeywords;

    public CategoryResolverService(string jsonPath)
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

    public ExpenseCategory Resolve(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
            return ExpenseCategory.Diger;

        var normalized = locationName.ToUpperInvariant();

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
