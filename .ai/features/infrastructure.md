# Infrastructure Layer

## Overview

Contains external service integrations. All infrastructure services are isolated from business logic and expose clean interfaces consumed by the service layer.

**Rule**: Every external integration must expose an interface. Never instantiate implementations directly — always inject via interface.

---

## Yahoo Finance Integration

### YahooMarketDataService
- Searches Yahoo Finance for securities by query string
- Returns metadata: name, ticker, type, exchange, currency, sector, industry, geography, market cap
- Used by `SecurityService.SearchAndCreate()` to auto-populate security records
- HTTP client configured with browser User-Agent to avoid blocks

### HistoricalPriceService
- Fetches historical OHLCV data (Open, High, Low, Close, Volume) for a ticker
- Used by rebalancing services to calculate price percentiles (1-year range)
- Endpoint: `https://query2.finance.yahoo.com/v8/finance/chart/{ticker}`

### Mappers
- **QuoteTypeMapper**: Converts Yahoo's `quoteType` (e.g., "EQUITY", "ETF", "CRYPTOCURRENCY") → domain `SecurityType` enum
- **GeographyMapper**: Infers geography from exchange code + currency (e.g., NYSE/NASDAQ → "North America", LSE → "Europe")

---

## HTTP Client Configuration

Yahoo Finance HTTP clients registered via `IHttpClientFactory` in the Worker's `ServiceCollectionExtensions`. The API project uses the same models but the Worker handles actual HTTP calls for price fetching.

---

## Test Strategy

Infrastructure services are tested via integration tests or with `HttpClient` mocks. No actual HTTP calls in unit tests.

---

## Adding a New Integration

1. Create `Infrastructure/{ServiceName}/` folder
2. Define interface (`IFooService`)
3. Implement service with HTTP client via `IHttpClientFactory`
4. Register in `ServiceCollectionExtensions`
5. Consume via interface injection in feature services
6. Add documentation to this file
