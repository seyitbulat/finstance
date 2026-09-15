using Finstance.dbContext;
using Finstance.Parsers;
using Finstance.Services;
using Finstance.Services.Resolvers;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UglyToad.PdfPig;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionStringBuilder = new NpgsqlConnectionStringBuilder
{
    Host = builder.Configuration["POSTGRES_HOST"] ?? "localhost",
    Port = int.TryParse(builder.Configuration["POSTGRES_PORT"], out var port) ? port : 5432,
    Database = builder.Configuration["POSTGRES_DB"],
    Username = builder.Configuration["POSTGRES_USER"],
    Password = builder.Configuration["POSTGRES_PASSWORD"],
    SslMode = SslMode.Require,
    TrustServerCertificate = true
};

builder.Services.AddDbContext<DataBaseContext>(options =>
{
    options.UseNpgsql(connectionStringBuilder.ConnectionString);
});

builder.Services.AddScoped<IBankStatementParser, YapiKrediParser>();
builder.Services.AddScoped<IBankStatementParser, QnbParser>();
builder.Services.AddScoped<StatementService>();

builder.Services.AddSingleton<ICategoryResolver, StringMatchCategoryResolver>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var jsonPath = Path.Combine(env.ContentRootPath, "categoryKeywords.json");
    return new StringMatchCategoryResolver(jsonPath);
});
builder.Services.AddSingleton<ICategoryResolver, FuzzyMatchCategoryResolver>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var jsonPath = Path.Combine(env.ContentRootPath, "categoryKeywords.json");
    return new FuzzyMatchCategoryResolver(jsonPath);
});

builder.Services.AddScoped<ILocationResolver, StringMatchResolver>();
builder.Services.AddScoped<ILocationResolver, FuzzyMatchResolver>();

builder.Services.AddScoped<LocationPipeline>();
builder.Services.AddScoped<CategoryPipeline>();
builder.Services.AddScoped<DataService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Bootstrap only explicitly opted-in development databases until migrations exist.
if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Database:Initialize"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
    await db.Database.EnsureCreatedAsync();
    scope.ServiceProvider.GetRequiredService<DataService>().SeedData();
}

// Configure the HTTP request pipeline.
app.UseCors();
app.UseHttpsRedirection();

// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(error.Error, "İşlenmeyen hata oluştu: {Message}", error.Error.Message);

            await context.Response.WriteAsJsonAsync(new
            {
                error = app.Environment.IsDevelopment()
                    ? error.Error.Message
                    : "Sunucuda bir hata oluştu. Lütfen daha sonra tekrar deneyin."
            });
        }
    });
});

async Task<int> GetOrCreateUserAsync(string username, DataBaseContext db)
{
    if (string.IsNullOrWhiteSpace(username))
        username = "guest";

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (user == null)
    {
        user = new Finstance.dbContext.Models.UserModel { Username = username, Password = "" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
    return user.Id;
}

app.MapPost("upload", async (HttpContext context, IFormFile file, StatementService statementService, DataService dataService, DataBaseContext db) =>
{
    var username = context.Request.Headers["X-User-Name"].FirstOrDefault() ?? "guest";
    var userId = await GetOrCreateUserAsync(username, db);

    if (file.Length == 0)
        return Results.BadRequest("Dosya boş.");

    if (file.Length > 20 * 1024 * 1024) // 20 MB limit
        return Results.BadRequest("Dosya boyutu 20 MB'ı aşamaz.");

    if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
        && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Sadece PDF dosyaları kabul edilmektedir.");

    using var stream = file.OpenReadStream();
    using var doc = PdfDocument.Open(stream, new ParsingOptions { ClipPaths = true });

    var (bankType, cutOffDate) = statementService.ExtractMetadata(doc);

    if (await dataService.IsStatementExistsAsync(cutOffDate, userId))
        return Results.Conflict("Bu ekstre zaten işlenmiş.");

    var result = statementService.Process(doc);

    await dataService.SaveAsync(result, userId);

    return TypedResults.Ok(result);
}).DisableAntiforgery();


app.MapGet("getMonthlyReport", async (HttpContext context, DateOnly date, DataService dataService, DataBaseContext db) =>
{
    var username = context.Request.Headers["X-User-Name"].FirstOrDefault() ?? "guest";
    var userId = await GetOrCreateUserAsync(username, db);

    var response = dataService.GetMonthlyReport(date, userId);

    return TypedResults.Ok(response);
});

app.MapGet("seed", (DataService dataService) =>
{
    dataService.SeedData();
    return Results.Ok("Seed verileri başarıyla yüklendi.");
});

app.Run();
