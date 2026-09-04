

using System.Text.Json;
using Finstance.dbContext.Models;
using Finstance.FuzzyMatch;
using Finstance.Services.Helpers;

namespace Finstance.Services.Resolvers;




public class FuzzyMatchCategoryResolver : ICategoryResolver
{
    public int Priority { get; set; } = 1;

    private readonly FuzzyScorer _fuzzyScorer;
    private readonly Dictionary<ExpenseCategory, List<string>> _categoryKeywords;

    public FuzzyMatchCategoryResolver(string jsonPath)
    {
        _fuzzyScorer = new();

        var json = File.ReadAllText(jsonPath);

        var raw = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);

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

    public async Task<ExpenseCategory?> ResolveAsync(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
            return ExpenseCategory.Diger;

        var normalized = NormalizerHelper.NormalizeTurkish(locationName);

        foreach (var (category, keywords) in _categoryKeywords)
        {
            foreach (var keyword in keywords)
            {
                if (_fuzzyScorer.PartialRatio(keyword, normalized) > 80)
                    return category;
            }
        }

        return null;
    }
}