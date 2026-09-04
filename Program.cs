using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Finstance.dbContext;
using Finstance.FuzzyMatch;
using Finstance.Parsers;
using Finstance.Services;
using Finstance.Services.Resolvers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tabula;
using Tabula.Detectors;
using Tabula.Extractors;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionStringBuilder = new NpgsqlConnectionStringBuilder
{
    Host = builder.Configuration["POSTGRES_HOST"] ?? "localhost",
    Port = int.TryParse(builder.Configuration["POSTGRES_PORT"], out var port) ? port : 5432,
    Database = builder.Configuration["POSTGRES_DB"],
    Username = builder.Configuration["POSTGRES_USER"],
    Password = builder.Configuration["POSTGRES_PASSWORD"]
};

builder.Services.AddDbContext<DataBaseContext>(options =>
{
    options.UseNpgsql(connectionStringBuilder.ConnectionString);
});



builder.Services.AddScoped<IBankStatementParser, YapiKrediParser>();
builder.Services.AddScoped<IBankStatementParser, QnbParser>();
builder.Services.AddScoped<StatementService>();



builder.Services.AddScoped<ILocationResolver, StringMatchResolver>();
builder.Services.AddScoped<ILocationResolver, FuzzyMatchResolver>();

builder.Services.AddScoped<LocationPipeline>();
builder.Services.AddScoped<DataService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();


app.MapPost("upload", async (IFormFile file, StatementService statementService, DataService dataService) =>
{
    using var stream = file.OpenReadStream();
    using var doc = PdfDocument.Open(stream, new ParsingOptions { ClipPaths = true });

    var (bankType, cutOffDate) = statementService.ExtractMetadata(doc);

    if (await dataService.IsStatementExistsAsync(cutOffDate))
        return Results.Conflict("Bu ekstre zaten işlenmiş.");

    var result = statementService.Process(doc);

    await dataService.SaveAsync(result);


    return TypedResults.Ok(result);
}).DisableAntiforgery();


app.MapGet("getMonthlyReport", (DateOnly date, DataService dataService) =>
{
    var response = dataService.GetMonthlyReport(date);

    return TypedResults.Ok(response);
});


app.MapGet("test", (DataService dataService) =>
{
  dataService.SeedData();
});


app.MapGet("getPatternLocations", (IFormFile file, StatementService statementService) =>
{
     using var stream = file.OpenReadStream();
    using var doc = PdfDocument.Open(stream, new ParsingOptions { ClipPaths = true });

    var (bankType, cutOffDate) = statementService.ExtractMetadata(doc);
});

app.Run();

