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


public class YapiKrediParser : IBankStatementParser
{
    private CultureInfo cultureInfo = new CultureInfo("tr-TR");

    private string bankType = "YapıKredi";
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

            if (pageText.Contains("yapı kredi", StringComparison.OrdinalIgnoreCase) || pageText.Contains("yapikredi", StringComparison.OrdinalIgnoreCase))
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
        stringBuilder.Append(words[indexOfAnchor + 2]);
        stringBuilder.Append(words[indexOfAnchor + 3]);
        stringBuilder.Append(words[indexOfAnchor + 4]);

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

                                DateOnly.TryParseExact(tarih, dateFormats, new CultureInfo("tr-TR"), DateTimeStyles.None, out date);

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






                                expenses.Add(new ExpenseModel(date, location, amount));

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
}