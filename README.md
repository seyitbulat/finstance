# Finstance

A backend API for parsing credit card statements from PDF files, storing the extracted expenses, and generating monthly reports.

## What It Does

Finstance reads credit card statement PDFs, extracts transaction data (date, merchant, amount), saves everything to a PostgreSQL database, and lets you query monthly spending reports through a simple API.

## Current Features

- **PDF Statement Parsing** — Upload a credit card statement PDF and get structured expense data back. The parser auto-detects the bank.
- **Supported Banks** — Yapı Kredi and QNB Finansbank.
- **Duplicate Detection** — Prevents the same statement from being processed twice (based on cut-off date).
- **Expense Storage** — Transactions are stored with date, location/merchant, and amount. Locations are tracked separately and deduplicated.
- **Expense Categories** — Locations can be categorized (groceries, restaurant, transport, clothing, entertainment, health, bills, other).
- **Monthly Reports** — Query expenses by month to get a breakdown of all transactions.

## Tech Stack

- .NET 10 / ASP.NET Core (Minimal APIs)
- PostgreSQL
- Entity Framework Core
- PdfPig + Tabula (PDF parsing)
- Docker Compose (for database)

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for PostgreSQL)

### Setup

1. Clone the repo:
   ```bash
   git clone https://github.com/seyitbulat/Finstance.git
   cd Finstance
   ```

2. Start the database:
   ```bash
   docker compose up -d
   ```

3. Copy the environment file and adjust if needed:
   ```bash
   cp .env.example .env
   ```

4. Run database migrations:
   ```bash
   dotnet ef database update
   ```

5. Run the app:
   ```bash
   dotnet run
   ```

## API Endpoints

### Upload Statement

```
POST /upload
Content-Type: multipart/form-data
```

Upload a credit card statement PDF. Returns the parsed expenses.

### Monthly Report

```
GET /getMonthlyReport?date=2026-08-01
```

Returns all expenses for the given month.

## Adding a New Bank Parser

Implement the `IBankStatementParser` interface:

```csharp
public interface IBankStatementParser
{
    bool CanHandle(PdfDocument doc);
    DateOnly ExtractCutOffDate(PdfDocument doc);
    CultureInfo GetCulture();
    List<ExpenseModel> ParseExpenses(PdfDocument doc);
    string GetBankType();
}
```

Register it in `Program.cs`:

```csharp
builder.Services.AddScoped<IBankStatementParser, YourBankParser>();
```

## Planned Features

- AI-powered spending analysis and insights
- Support for more Turkish banks
- User authentication
- Spending trends and visualizations
- Expense category auto-classification

