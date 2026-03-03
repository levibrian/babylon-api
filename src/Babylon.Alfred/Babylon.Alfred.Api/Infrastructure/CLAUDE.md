# Infrastructure Layer

## Overview

Contains external service integrations. Currently the only integration is Yahoo Finance for market data. Infrastructure services are isolated from business logic and expose clean interfaces consumed by the service layer.

## Structure

```
Infrastructure/
└── YahooFinance/
    ├── Services/
    │   ├── YahooMarketDataService.cs        # Searches Yahoo Finance for security metadata
    │   ├── IHistoricalPriceService.cs       # Interface for historical price data
    │   └── HistoricalPriceService.cs        # Fetches historical OHLCV price data
    ├── Models/
    │   ├── YahooResponse.cs                 # API response deserialization models
    │   └── YahooSearchResult.cs             # Search result model
    └── Mappers/
        ├── QuoteTypeMapper.cs               # Maps Yahoo QuoteType string to SecurityType enum
        └── GeographyMapper.cs               # Maps exchange/currency to geography string
```

## Yahoo Finance Integration

### YahooMarketDataService
- Searches Yahoo Finance for securities by query string.
- Returns metadata: name, ticker, type, exchange, currency, sector, industry, geography, market cap.
- Used by `SecurityService.SearchAndCreate()` to auto-populate security records.
- HTTP client configured with browser User-Agent to avoid blocks.

### HistoricalPriceService
- Fetches historical price data for a ticker over configurable periods.
- Used by rebalancing services to calculate price percentiles (1Y range).
- Returns OHLCV data (Open, High, Low, Close, Volume).
- Yahoo Finance v8 API: `https://query2.finance.yahoo.com/v8/finance/chart/{ticker}`

### Mappers
- **QuoteTypeMapper**: Converts Yahoo's `quoteType` (e.g., "EQUITY", "ETF", "CRYPTOCURRENCY") to the domain `SecurityType` enum.
- **GeographyMapper**: Infers geography from exchange code and currency (e.g., NYSE/NASDAQ -> "North America", LSE -> "Europe").

## Adding a New External Integration

1. Create a folder under `Infrastructure/{ServiceName}/`.
2. Add subfolders: `Services/`, `Models/`, `Mappers/` as needed.
3. Define an interface for the service (e.g., `IFooService`).
4. Register the HTTP client and service in `ServiceCollectionExtensions`.
5. Consume the interface in feature services via constructor injection.

## HTTP Client Configuration

Yahoo Finance HTTP clients are registered via `IHttpClientFactory` in the Worker's `ServiceCollectionExtensions`. The API project uses the same models but the Worker handles the actual HTTP calls for price fetching.
