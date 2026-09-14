using System.Globalization;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using Finstance.Models;
using Tabula;
using Tabula.Detectors;
using Tabula.Extractors;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Finstance.Parsers;


public class QnbParser : IBankStatementParser
{
    private CultureInfo cultureInfo = CultureInfo.InvariantCulture;

    private string bankType = "Qnb";


     private static readonly Regex DateRegex = new(@"(?i)\b(\d{1,2})[\s/.-]+(ocak|şubat|mart|nisan|mayıs|haziran|temmuz|ağustos|eylül|ekim|kasım|aralık|\d{1,2})[\s/.-]+(\d{4})\b", RegexOptions.Compiled);
    private static readonly Regex InstalmentRegex =
 new(@"(?<current>\d{1,2})\s*/\s*(?<total>\d{1,2})\s*$", RegexOptions.Compiled);
    private static readonly Regex AmountRegex =
                new(@"([+-])?\d{1,3}(?:[.,]\d{3})*[.,]\d{2}\b", RegexOptions.Compiled);
    private string[] dateFormats =
    {
        "d MMMM yyyy",
        "dd MMMM yyyy",
        "dMMMMyyyy",
        "ddMMMMyyyy",
        "dd/MM/yyyy"
    };
    public bool CanHandle(PdfDocument doc)
    {
        for (int i = 1; i <= doc.NumberOfPages; i++)
        {
            string pageText = doc.GetPage(i).Text;

            if (pageText.Contains("qnb", StringComparison.OrdinalIgnoreCase))
            {

                return true;
            }
        }

        return false;
    }

    public DateOnly ExtractCutOffDate(PdfDocument doc)
    {
        var words = doc.GetPage(1).GetWords().ToList();

        if (words == null || words.Count == 0)
        {
            // error message
        }
        var anchorWord = words.Where(x => x.Text == "Kesim").FirstOrDefault();

        if (anchorWord == null)
        {
            // error message
        }



        var indexOfAnchor = words.IndexOf(anchorWord);
        DateOnly cutOffDate = DateOnly.MinValue;

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(words[indexOfAnchor + 3]);

        DateOnly.TryParseExact(stringBuilder.ToString(), dateFormats, new CultureInfo("tr-TR"), DateTimeStyles.None, out cutOffDate);

        return cutOffDate;
    }

    public CultureInfo GetCulture()
    {
        return cultureInfo;
    }

    public List<ExpenseModel> ParseExpenses(PdfDocument doc)
    {
        var expenses = new List<ExpenseModel>();
        for (int i = 1; i <= doc.NumberOfPages; i++)
        {
            PageArea page = ObjectExtractor.Extract(doc, i);

            SimpleNurminenDetectionAlgorithm detector = new SimpleNurminenDetectionAlgorithm();
            var regions = detector.Detect(page);

            if (regions == null || regions.Count == 0) continue;

            foreach (var region in regions)
            {
                IExtractionAlgorithm ea = new BasicExtractionAlgorithm();
                IReadOnlyList<Table> tables = ea.Extract(page.GetArea(region.BoundingBox));

                if (tables == null || tables.Count == 0) continue;
                string pattern = @"(?i)\b(\d{1,2})[\s/.-]+(ocak|şubat|mart|nisan|mayıs|haziran|temmuz|ağustos|eylül|ekim|kasım|aralık|\d{1,2})[\s/.-]+(\d{4})\b";
                foreach (var table in tables)
                {
                    var rows = table.Rows;

                    foreach (var row in rows)
                    {
                        var cells = row.Select(x => x.GetText().Trim()).ToList();

                        int dateIndex = cells.FindIndex(c => Regex.IsMatch(c, pattern, RegexOptions.IgnoreCase));

                        if (dateIndex != -1)
                        {
                            if (dateIndex + 3 < cells.Count)
                            {
                                string firstCell = cells[dateIndex];

                                string tarih = Regex.Matches(firstCell, pattern, RegexOptions.IgnoreCase).FirstOrDefault().Value;

                                DateOnly date = DateOnly.MinValue;

                                DateOnly.TryParseExact(tarih, dateFormats, cultureInfo, DateTimeStyles.None, out date);

                                string location = "NULL";
                                if (firstCell.Length > tarih.Length)
                                {
                                    location = firstCell.Substring(tarih.Length);
                                }
                                else
                                {
                                    location = cells[dateIndex + 1];
                                }


                                int index = dateIndex + 2;
                                decimal amount = 0;
                                string amountString = "NULL";
                                var isInstalment = false;
                                var amountIndex = 0;

                                while (!decimal.TryParse(cells[index], cultureInfo, out amount))
                                {
                                    if (index + 1 < cells.Count)
                                    {
                                        index++;

                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                amountString = amount.ToString();


                                if ((amountIndex + 1) <= cells.Count)
                                {
                                    if (cells[amountIndex + 1].Contains("/"))
                                    {
                                        isInstalment = true;
                                    }
                                }




                                expenses.Add(new ExpenseModel(date, location, amount, isInstalment));

                            }
                        }
                    }
                }


            }


        }

        return expenses;
    }

    public string GetBankType()
    {
        return bankType;
    }

    public List<ExpenseModel> ParseExpensesNonTabular(PdfDocument doc)
    {
        var expenses = new List<ExpenseModel>();
        foreach (var page in doc.GetPages())
        {
            var lines = GroupIntoLines(page.GetWords().ToList());

            foreach (var line in lines)
            {
                var lineText = string.Join(" ", line.OrderBy(x => x.BoundingBox.Left).Select(x => x.Text));
                var dateMatch = DateRegex.Match(lineText);
                var amountMatch = AmountRegex.Match(lineText);

                if (!dateMatch.Success || !amountMatch.Success)
                    continue;

                var amountText = amountMatch.Value;
                var isCredit = amountText.TrimStart().StartsWith("-");

                if (isCredit)
                    continue;

                DateOnly date;
                DateOnly.TryParse(dateMatch.Value, out date);

                Decimal? amount = TryParseAmount(amountMatch.Value);;
                
                if(amount == null)
                    amount = 0;
                    
                bool isInstalment = false;

                if (InstalmentRegex.Match(lineText).Success)
                    isInstalment = true;




                var name = lineText.Replace(dateMatch.Value, "").Replace(amountText, "").Trim();

                expenses.Add(new ExpenseModel(date, name, amount.Value, isInstalment));


            }

        }

        return expenses;
    }

        private List<List<Word>> GroupIntoLines(List<Word> words)
    {
        var sorted = words.OrderByDescending(x => x.BoundingBox.Bottom).ToList();

        var lines = new List<List<Word>>();

        foreach (var word in sorted)
        {
            var line = lines.FirstOrDefault(l =>
                Math.Abs(l.First().BoundingBox.Bottom - word.BoundingBox.Bottom) <= 3.0);

            if (line != null)
                line.Add(word);
            else
                lines.Add(new List<Word> { word });
        }

        return lines;
    }

     private decimal? TryParseAmount(string raw)
        {
            raw = raw.Trim();
 
            var lastComma = raw.LastIndexOf(',');
            var lastDot = raw.LastIndexOf('.');
 
            string normalized;
 
            if (lastComma > lastDot)
            {
                // "1.234,56" -> "1234.56"
                normalized = raw.Replace(".", "").Replace(",", ".");
            }
            else if (lastDot > lastComma)
            {
                // "1,234.56" -> "1234.56"
                normalized = raw.Replace(",", "");
            }
            else
            {
                normalized = raw.Replace(",", ".");
            }
 
            return decimal.TryParse(normalized, NumberStyles.Any,
                CultureInfo.InvariantCulture, out var result) ? result : null;
        }
}