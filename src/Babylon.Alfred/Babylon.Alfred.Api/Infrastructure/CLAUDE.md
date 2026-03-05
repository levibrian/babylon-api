# Infrastructure Layer

## Overview

Contains external service integrations. Currently the only integration is Yahoo Finance for market data. Infrastructure services are isolated from business logic and expose clean interfaces consumed by the service layer.

## Structure

### YahooFinance Integration
- **Services**: `YahooMarketDataService` (search), `HistoricalPriceService` (OHLCV data)
- **Models**: `YahooResponse`, `YahooSearchResult`
- **Mappers**: `QuoteTypeMapper` (Yahoo type → SecurityType enum), `GeographyMapper` (exchange → geography)

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

## Integration Contract

**Rule**: Every external integration must expose an interface consumed by feature services. The implementation is injected — never instantiated directly.

### Adding a New Integration
1. Create `Infrastructure/{ServiceName}/` folder
2. Define interface (e.g., `IFooService`)
3. Implement service with HTTP client via `IHttpClientFactory`
4. Register in `ServiceCollectionExtensions`
5. Consume via interface injection in feature services

## HTTP Client Configuration

Yahoo Finance HTTP clients are registered via `IHttpClientFactory` in the Worker's `ServiceCollectionExtensions`. The API project uses the same models but the Worker handles actual HTTP calls for price fetching.

## Test Strategy

Infrastructure services are tested via integration tests or with `HttpClient` mocks. No actual HTTP calls in unit tests.
