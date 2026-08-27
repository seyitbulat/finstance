using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Finstance.Parsers;
using Finstance.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Tabula;
using Tabula.Detectors;
using Tabula.Extractors;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddScoped<IBankStatementParser, YapiKrediParser>();
builder.Services.AddScoped<IBankStatementParser, QnbParser>();
builder.Services.AddScoped<StatementService>();


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();




app.MapPost("upload", async (IFormFile file, StatementService service) =>
{
    using var stream = file.OpenReadStream();

    using var doc = PdfDocument.Open(stream, new ParsingOptions { ClipPaths = true});

    var result = service.Process(doc);

    return TypedResults.Ok(result);

}).DisableAntiforgery();

app.Run();

