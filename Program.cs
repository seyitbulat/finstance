using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http.HttpResults;
using Tabula;
using Tabula.Detectors;
using Tabula.Extractors;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapPost("/upload", async (IFormFile file) =>
{
	using var stream = file.OpenReadStream();

	using var document = PdfDocument.Open(stream);

	var text = new StringBuilder();

	foreach (var page in document.GetPages())
	{
		text.AppendLine($"--- SAYFA {page.Number} ---");
		text.AppendLine(page.Text);
	}

	return TypedResults.Ok(new
	{
		FileName = file.FileName,
		PageCount = document.NumberOfPages,
		ExtractedText = text.ToString()
	});

}).DisableAntiforgery();


app.MapPost("upload2", async (IFormFile file) =>
{

	string[] dateFormats =
	{
		"d MMMM yyyy",
		"dd MMMM yyyy",
		"dMMMMyyyy",
		"ddMMMMyyyy",
		"dd/MM/yyyy"
	};


	using var stream = file.OpenReadStream();

	using var document = PdfDocument.Open(stream, new ParsingOptions { ClipPaths = true });

	List<Expense> expenses = new();


	Bank bank = Bank.UNKNOWN;

	for (int i = 1; i <= document.NumberOfPages; i++)
	{
		string pageText = document.GetPage(i).Text;


		if (pageText.Contains("yapı kredi", StringComparison.OrdinalIgnoreCase) || pageText.Contains("yapikredi", StringComparison.OrdinalIgnoreCase))
		{
			bank = Bank.YAPIKREDI;

			break;
		}
		else if (pageText.Contains("qnb", StringComparison.OrdinalIgnoreCase))
		{
			bank = Bank.QNB;
			break;
		}
	}

	List<Tuple<string, string>> res = new();



	var firstPage = document.GetPage(1);

	var words = firstPage.GetWords().ToList();
	var indexOfAnchor = words.IndexOf(words.Where(x => x.Text == "Kesim").FirstOrDefault());

	DateOnly cutOffDate = DateOnly.MinValue;

	if (bank == Bank.YAPIKREDI)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(words[indexOfAnchor + 2]);
		stringBuilder.Append(words[indexOfAnchor + 3]);
		stringBuilder.Append(words[indexOfAnchor + 4]);

		DateOnly.TryParseExact(stringBuilder.ToString(), dateFormats, new CultureInfo("tr-TR"), DateTimeStyles.None, out cutOffDate);

	}
	else if (bank == Bank.QNB)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(words[indexOfAnchor + 3]);

		DateOnly.TryParseExact(stringBuilder.ToString(), dateFormats, new CultureInfo("tr-TR"), DateTimeStyles.None, out cutOffDate);

	}
	for (int i = 1; i <= document.NumberOfPages; i++)
	{
		PageArea page = ObjectExtractor.Extract(document, i);

		SimpleNurminenDetectionAlgorithm detector = new SimpleNurminenDetectionAlgorithm();
		var regions = detector.Detect(page);

		if (regions == null || regions.Count == 0) continue;

		foreach (var region in regions)
		{
			IExtractionAlgorithm ea = new BasicExtractionAlgorithm();
			IReadOnlyList<Table> tables = ea.Extract(page.GetArea(region.BoundingBox));

			if (tables == null || tables.Count == 0) continue;
			// string pattern = @"(?i)\b(\d{1,2})\s+(ocak|şubat|mart|nisan|mayıs|haziran|temmuz|ağustos|eylül|ekim|kasım|aralık)\s+(\d{4})\b";
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
							double amount = 0;
							string amountString = "NULL";
							var culture = new CultureInfo("tr-TR");

							if (bank == Bank.QNB)
							{
								culture = CultureInfo.InvariantCulture;
							}
							while (!double.TryParse(cells[index], culture, out amount))
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






							expenses.Add(new Expense(tarih, date,location, amount));

						}
					}
				}
			}
		}



	}


	return TypedResults.Ok(new
	{
		FileName = file.FileName,
		PageCount = document.NumberOfPages,
		CutOffDate = cutOffDate,
		BankType = bank == Bank.YAPIKREDI ? "Yapı Kredi" :
				  bank == Bank.QNB ? "QNB" :
				  "Bilinmiyor",
		Data = expenses
	});
}).DisableAntiforgery();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}



record Expense(string DateStr, DateOnly Date, string Location, double Amount)
{
}

enum Bank
{
	UNKNOWN,
	YAPIKREDI,
	QNB
}
