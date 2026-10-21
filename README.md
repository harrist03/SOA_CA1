# Portfolio Tracker

A Blazor Server portfolio tracker built with .NET 10. The application loads portfolio holdings from JSON, retrieves live market data, converts displayed values into the selected currency, and lets users add, edit, and remove holdings.

## Features

- View portfolio total value, total cost, and total gain/loss.
- View each holding's:
  - ticker symbol and company name
  - company logo
  - quantity
  - buy price
  - current price
  - market value
  - gain/loss and gain/loss percentage
- Search for common stocks before adding them.
- Add, edit, and remove holdings.
- Persist holding changes to `SOA_CA1/Data/holdings.json`.
- Sort holdings by symbol, quantity, buy price, current price, market value, or gain/loss percentage.
- Display values in USD, EUR, GBP, JPY, or CNY.
- Open a stock-detail page for a holding.
- View the current quote and recent company news for a stock.
- Handle unavailable quotes without hiding the rest of the portfolio.
- Use validation and user-facing error messages for invalid input and failed API or file operations.

## Technology

- .NET 10
- Blazor Server with interactive server rendering
- C#
- Bootstrap 5
- xUnit for unit testing

## Project structure

```text
SOA_CA1/
├── Components/
│   ├── Layout/
│   └── Pages/
├── Data/
│   └── holdings.json
├── Models/
├── Services/
├── wwwroot/
├── Program.cs
└── SOA_CA1.csproj

SOA_CA1.Tests/
├── CurrencyServiceTests.cs
├── HoldingsRepositoryTests.cs
├── MarketDataServiceTests.cs
├── PortfolioServiceTests.cs
└── SOA_CA1.Tests.csproj
```

## Domain models

### `Portfolio`

Represents the complete portfolio:

- portfolio name
- creation date
- collection of holdings
- calculated total cost
- calculated total value
- calculated total gain/loss

The calculated totals are runtime values and are not written to JSON.

### `Holding`

Represents one holding in the portfolio:

- unique identifier
- ticker symbol
- company name
- quantity
- buy price
- date bought
- live quote
- exchange-rate multiplier
- calculated cost basis, market value, gain/loss, and gain/loss percentage

The live quote and exchange-rate multiplier are runtime-only values. They are marked with `JsonIgnore` and are recalculated when the portfolio is loaded.

### `StockQuote`

Maps the quote response returned by Finnhub, including:

- current price
- price change
- percentage change
- daily high and low
- opening price
- previous close
- quote timestamp

### `NewsArticle`

Represents a company-news item:

- headline
- summary
- source
- article URL
- publication timestamp

### `SearchResult`

Represents a stock-search result:

- ticker symbol
- company description
- security type

The application filters search results to common stocks and excludes symbols containing a period.

### `SortField`

Defines the supported portfolio sort fields:

- symbol
- market value
- gain/loss percentage
- quantity
- buy price
- current price

## Services and interfaces

### `IMarketDataService` / `MarketDataService`

Provides Finnhub operations:

```csharp
Task<IReadOnlyList<SearchResult>> SearchAsync(string query);
Task<StockQuote> GetQuoteAsync(string symbol);
Task<IReadOnlyList<NewsArticle>> GetNewsAsync(string symbol, int days = 7);
```

Responsibilities include:

- normalizing ticker symbols
- validating responses
- converting HTTP, network, and JSON failures into `ApiException`
- filtering stock-search results
- caching successful quote responses for five minutes

### `ICurrencyService` / `CurrencyService`

Retrieves the latest exchange rate between two currencies:

```csharp
Task<decimal> GetRateAsync(string from, string to);
```

Currency codes are trimmed and normalized to uppercase before the request is sent.

### `IHoldingsRepository` / `HoldingsRepository`

Loads and saves the portfolio in `Data/holdings.json`:

```csharp
Task<Portfolio> LoadAsync();
Task SaveAsync(Portfolio portfolio);
```

Persistence behavior:

- A missing file produces an empty portfolio.
- Invalid JSON produces an `InvalidDataException`.
- Saves are written to a uniquely named temporary file first.
- The existing JSON file is replaced only after serialization succeeds.
- `SemaphoreSlim` prevents concurrent writes from interfering with one another.
- Temporary files are removed if a save fails.

### `IPortfolioService` / `PortfolioService`

Coordinates repository, market-data, and currency services:

```csharp
Task<Portfolio> GetPortfolioAsync(string displayCurrency);
IEnumerable<Holding> Sort(
    IEnumerable<Holding> holdings,
    string sortBy,
    bool descending);
```

When loading a portfolio, it:

1. Loads the durable holdings from the repository.
2. Retrieves the selected display-currency rate from USD.
3. Applies the rate to each holding.
4. Retrieves each live quote.
5. Leaves an individual quote unavailable if that quote request fails, while keeping the holding visible.

### `CurrencyPreference`

Stores the selected currency for the current Blazor circuit and maps currency codes to display symbols:

- USD: `$`
- EUR: `€`
- GBP: `£`
- JPY: `¥`
- CNY: `¥`

## External providers and endpoints

### Finnhub

Finnhub provides stock search, quotes, and company news.

Base URL:

```text
https://finnhub.io/api/v1/
```

Endpoints used:

| Operation | Endpoint |
|---|---|
| Stock search | `GET /search?q={query}&token={apiKey}` |
| Quote | `GET /quote?symbol={symbol}&token={apiKey}` |
| Company news | `GET /company-news?symbol={symbol}&from={yyyy-MM-dd}&to={yyyy-MM-dd}&token={apiKey}` |

Quote responses are cached in memory for five minutes using a normalized-symbol cache key.

### Frankfurter

Frankfurter provides the latest foreign-exchange rate used to convert USD portfolio data into the selected display currency.

Base URL:

```text
https://api.frankfurter.dev/v2/
```

Endpoint used:

```text
GET /rates?base={fromCurrency}&quotes={toCurrency}
```

USD uses a rate of `1` locally, so the application does not call Frankfurter when USD is selected.

### Logo.dev

Logo.dev provides company logos through its image CDN. The application does not call the Logo.dev JSON API; the browser requests an image URL directly.

```text
https://img.logo.dev/ticker/{symbol}?token={publishableKey}&size={size}
```

Logo usage:

- portfolio table: small logo
- stock-detail page: larger logo

The Logo.dev CDN publishable key is safe to include in image URLs. A secret Logo.dev API key must not be placed in client-side markup or committed to the repository.

## Configuration and secrets

The project uses ASP.NET Core User Secrets for local API keys. The project file contains a `UserSecretsId`, so initialize or use the existing secret store with:

```powershell
dotnet user-secrets set "Finnhub:ApiKey" "your-finnhub-key" `
    --project ".\SOA_CA1\SOA_CA1.csproj"

dotnet user-secrets set "LogoDev:PublishableKey" "your-logo-dev-publishable-key" `
    --project ".\SOA_CA1\SOA_CA1.csproj"
```

Never commit API keys, publishable keys, or the local `secrets.json` file.

## Running the application

From the repository root:

```powershell
dotnet run --project ".\SOA_CA1\SOA_CA1.csproj"
```

Open the HTTPS URL printed by ASP.NET Core. The main portfolio page is `/`. Stock-detail pages use:

```text
/stock/{symbol}
```

If `dotnet` is not available on `PATH`, install the .NET 10 SDK or run the executable from its installed location.

## Running tests

```powershell
dotnet test ".\SOA_CA1.Tests\SOA_CA1.Tests.csproj"
```

The tests use local fakes and stub HTTP handlers. They do not call Finnhub, Frankfurter, or Logo.dev, and they do not modify the real `Data/holdings.json` file.

Current test coverage includes:

- Finnhub search filtering and error handling
- quote retrieval and five-minute cache behavior
- invalid quote handling
- company-news retrieval and failures
- Frankfurter rate parsing and failures
- portfolio currency conversion
- quote-failure isolation
- portfolio sorting
- holdings JSON loading, missing-file behavior, corrupt-file behavior, saving, and runtime-only property exclusion
