# Babylon Alfred API 🏦

A personal investment tracking API built with ASP.NET Core that helps manage and monitor stock portfolio transactions. Named after Batman's loyal butler, Alfred, this API serves as your automated assistant for investment management.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## 📋 Table of Contents

- [Domain Objective](#-domain-objective)
- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Architecture](#-architecture)
- [Database Models](#-database-models)
- [API Structure](#-api-structure)
- [Services & Responsibilities](#-services--responsibilities)
- [Getting Started](#-getting-started)
- [Development](#-development)
- [Testing](#-testing)
- [Deployment](#-deployment)
- [Code Style & Conventions](#-code-style--conventions)
- [Business Rules](#-business-rules)
- [Roadmap](#-roadmap)
- [Contributing](#-contributing)

## 🎯 Domain Objective

**Babylon Alfred API** is a personal investment tracking and portfolio management system. The system helps users track their stock portfolio transactions, monitor positions, calculate portfolio metrics, and manage allocation strategies.

### Core Business Capabilities
- **Transaction Management**: Record buy/sell transactions with detailed pricing, fees, and dates
- **Portfolio Tracking**: View aggregated portfolio positions with real-time calculations
- **Security Management**: Maintain a database of securities (stocks, ETFs, bonds, crypto, etc.)
- **Allocation Strategy**: Set target allocation percentages per security and track rebalancing needs
- **Portfolio Analytics**: Calculate total invested, position sizes, average cost basis, and rebalancing recommendations
- **Multi-User Support**: Track investments for multiple users (currently using a root user constant)

## ✨ Features

### Investment Management
- 📊 **Portfolio Tracking**: View aggregated portfolio positions with real-time calculations
- 💰 **Transaction Management**: Record buy/sell transactions with detailed pricing and fees
- 🏢 **Security Database**: Maintain metadata for tracked securities (stocks, ETFs, bonds, crypto, etc.)
- 👤 **Multi-User Support**: Track investments for multiple users
- 📈 **Allocation Strategies**: Set target allocation percentages and track rebalancing needs

### Technical Features
- 🔄 **Bulk Operations**: Import multiple transactions at once
- 📈 **Portfolio Analytics**: Calculate total invested, position sizes, and transaction history
- 🤖 **Telegram Bot Integration**: (Work in progress) Manage investments via Telegram
- 🏥 **Health Checks**: Monitor API availability
- 📝 **Swagger Documentation**: Interactive API documentation
- 🔄 **Background Jobs**: Scheduled price fetching via Worker service

## 🛠 Tech Stack

### Framework & Language
- **.NET 9.0**
- **C# 12** with nullable reference types enabled
- **ASP.NET Core** Web API
- **Entity Framework Core 8.0** (ORM)
- **PostgreSQL 16+** (Database)

### Key NuGet Packages
- **EFCore.BulkExtensions** (v8.1.3) - Bulk operations
- **EFCore.BulkExtensions.PostgreSql** (v8.1.3)
- **Newtonsoft.Json** (v13.0.3) - JSON serialization with custom converters
- **Serilog.AspNetCore** (v8.0.3) - Structured logging
- **Swashbuckle.AspNetCore** (v6.4.0) - Swagger/OpenAPI
- **Telegram.Bot** (v22.6.2) - Telegram integration (in progress)
- **Quartz** - Scheduled jobs (Worker service)

### Testing Stack
- **xUnit** (v2.5.3) - Test framework
- **Moq** (v4.20.72) - Mocking framework
- **Moq.AutoMock** (v3.5.0) - Auto-mocking container
- **AutoFixture** (v4.18.1) - Test data generation
- **FluentAssertions** (v8.8.0) - Fluent assertion library
- **Microsoft.EntityFrameworkCore.InMemory** (v8.0.13) - In-memory database for tests

### Infrastructure
- **AWS** (RDS PostgreSQL, VPC)
- **Terraform** (Infrastructure as Code)
- **Docker & Docker Compose**
- **Serilog** (Console, File sinks)

## 🏗 Architecture

### Vertical Slice Architecture
The project uses **feature-based organization** (Vertical Slice Architecture) where each feature is self-contained:

```
Features/
├── Investments/          # Investment management feature
│   ├── Controllers/      # API endpoints
│   ├── Services/         # Business logic
│   ├── Models/           # DTOs (Requests/Responses)
│   └── Shared/           # Feature-specific shared code
├── Telegram/             # Telegram bot feature
└── Startup/              # Health checks, etc.
```

### Design Patterns
1. **Repository Pattern**: Data access abstraction (`ITransactionRepository`, `ISecurityRepository`, etc.)
2. **Dependency Injection**: ASP.NET Core built-in DI container
3. **Service Layer**: Business logic separated from controllers
4. **DTO Pattern**: Request/Response models separate from domain entities
5. **Middleware Pattern**: Global error handling and request logging

### Service Registration
Services are registered via extension methods in `Features/Startup/Extensions/ServiceCollectionExtensions.cs`:
- Repositories: Scoped lifetime
- Services: Scoped lifetime
- All registered via `RegisterFeatures()` extension method

## 📊 Database Models

### Core Entities

#### 1. **Security** (formerly Company)
```csharp
public class Security
{
    public Guid Id { get; set; }                    // Primary key
    public string Ticker { get; set; }             // Unique ticker symbol (e.g., "AAPL")
    public string SecurityName { get; set; }       // Display name
    public SecurityType SecurityType { get; set; } // Enum: Stock, ETF, MutualFund, Bond, Crypto, REIT, Options, Commodity
    public DateTime? LastUpdated { get; set; }     // Last update timestamp

    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; }
    public ICollection<AllocationStrategy> AllocationStrategies { get; set; }
}
```

**Table**: `securities`
**Unique Index**: `Ticker`
**SecurityType Enum**: Stored as integer (1-8)

#### 2. **Transaction**
```csharp
public class Transaction
{
    public Guid Id { get; set; }
    public Guid SecurityId { get; set; }           // FK to Security
    public TransactionType TransactionType { get; set; } // Buy or Sell
    public DateTime Date { get; set; }             // Transaction date
    public DateTime UpdatedAt { get; set; }        // Last update timestamp (used for ordering)
    public decimal SharesQuantity { get; set; }     // Number of shares
    public decimal SharePrice { get; set; }         // Price per share
    // Price per share
    public decimal Fees { get; set; }               // Transaction fees
    public Guid? UserId { get; set; }              // FK to User (nullable, defaults to RootUserId)

    // Navigation properties
    public User? User { get; set; }
    public Security Security { get; set; }

    // Computed properties (NotMapped)
    public decimal Amount => SharesQuantity * SharePrice;
    public decimal TotalAmount => Amount + Fees;
}
```

**Table**: `transactions`
**Indexes**: `SecurityId`, `UserId`
**TransactionType Enum**: Buy = 0, Sell = 1

#### 3. **User**
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }

    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; }
}
```

**Table**: `users`
**Root User Constant**: `a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d` (defined in `Constants.User.RootUserId`)

#### 4. **AllocationStrategy**
```csharp
public class AllocationStrategy
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }               // FK to User
    public Guid SecurityId { get; set; }           // FK to Security
    public decimal TargetPercentage { get; set; }  // Target allocation % (0-100)
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; }
    public Security Security { get; set; }
}
```

**Table**: `allocation_strategies`
**Unique Index**: `(UserId, SecurityId)` - One allocation strategy per user per security

#### 5. **MarketPrice**
```csharp
public class MarketPrice
{
    public Guid Id { get; set; }
    public string Ticker { get; set; }             // Ticker symbol (not FK, just string)
    public decimal Price { get; set; }            // Current market price
    public DateTime LastUpdated { get; set; }     // Price update timestamp
}
```

**Table**: `market_prices`
**Note**: Uses `Ticker` as string, not FK to Security (allows flexibility for different exchanges)

### Entity Relationships

```
User (1) ────< (Many) Transaction
Security (1) ────< (Many) Transaction
Security (1) ────< (Many) AllocationStrategy
User (1) ────< (Many) AllocationStrategy
```

### Database Context
- **DbContext**: `BabylonDbContext`
- **Configurations**: Each entity has a `*Configuration.cs` file using Fluent API
- **Migrations**: Located in `Shared/Data/Migrations/`
- **Factory**: `BabylonDbContextFactory` for design-time migrations

## 🔌 API Structure

### Base URL
- **Development**: `https://localhost:7192` or `http://localhost:5000`
- **Production**: Configured via environment variables

### API Versioning
All endpoints use `/api/v1/` prefix.

### Controllers

#### 1. **SecuritiesController** (`/api/v1/securities`)
- `GET /` - Get all securities
- `GET /{ticker}` - Get security by ticker
- `POST /` - Create new security
- `PUT /{ticker}` - Update security
- `DELETE /{ticker}` - Delete security

#### 2. **TransactionsController** (`/api/v1/transactions`)
- `POST /` - Create single transaction
- `POST /bulk` - Create multiple transactions
- `GET /{userId?}` - Get all transactions for user (ordered by UpdatedAt descending)
- `PUT /{userId}/{transactionId}` - Update transaction
- `DELETE /{userId}/{transactionId}` - Delete transaction

#### 3. **PortfoliosController** (`/api/v1/portfolios`)
- `GET /{userId}` - Get user portfolio with positions, allocations, and rebalancing info

#### 4. **AllocationController** (`/api/v1/allocation`)
- `GET /{userId}` - Get allocation strategies for user
- `POST /{userId}` - Set allocation strategy

#### 5. **InsightsController** (`/api/v1/insights`)
- `GET /{userId}` - Get portfolio insights (rebalancing recommendations)

#### 6. **HealthController** (`/health`)
- `GET /` - Health check endpoint

### Request/Response Models

#### Request DTOs
- `CreateTransactionRequest`: Ticker, TransactionType, Date, SharesQuantity, SharePrice, Fees, UserId (optional)
- `UpdateTransactionRequest`: All fields optional (partial update)
- `CreateCompanyRequest`: Ticker, SecurityName, SecurityType
- `UpdateCompanyRequest`: SecurityName, SecurityType (optional)
- `SetAllocationStrategyRequest`: Ticker, TargetPercentage

#### Response DTOs
- `TransactionDto`: Id, Ticker, SecurityName, Date, SharesQuantity, SharePrice, Fees, TransactionType, TotalAmount
- `CompanyDto`: Ticker, SecurityName, SecurityType
- `PortfolioResponse`: Positions (List<PortfolioPositionDto>), TotalInvested
- `PortfolioPositionDto`: Ticker, SecurityName, TotalInvested, TotalShares, AverageSharePrice, CurrentAllocationPercentage, TargetAllocationPercentage, AllocationDeviation, RebalancingAmount, RebalancingStatus, CurrentMarketValue, Transactions (last 5)
- `AllocationStrategyDto`: Ticker, SecurityName, TargetPercentage

### JSON Serialization
- Uses **Newtonsoft.Json** (not System.Text.Json)
- Custom converters: `StringEnumConverter`, `UnixDateTimeConverter`
- Configured in `Program.cs`

### CORS Configuration
- Configured in `appsettings.json` and `appsettings.Development.json`
- Default origins: `http://localhost:3000` (development)
- Allows credentials, any method, any header

## 🧩 Services & Responsibilities

### Repository Layer (Data Access)

#### `ITransactionRepository` / `TransactionRepository`
- `Add(Transaction)` - Add single transaction
- `AddBulk(IList<Transaction>)` - Bulk insert transactions
- `GetAll()` - Get all transactions
- `GetOpenPositionsByUser(Guid)` - Get transactions with open positions (excludes fully sold positions)
- `GetAllByUser(Guid)` - Get all transactions for user (ordered by UpdatedAt descending)
- `GetById(Guid, Guid)` - Get transaction by ID and UserId
- `Update(Transaction)` - Update transaction
- `Delete(Guid, Guid)` - Delete transaction by ID and UserId

**Key Logic**: `GetOpenPositionsByUser` filters out positions where total shares = 0 (fully sold).

#### `ISecurityRepository` / `SecurityRepository`
- `GetByTickerAsync(string)` - Get security by ticker
- `GetByTickersAsync(IEnumerable<string>)` - Get multiple securities by tickers (returns Dictionary)
- `GetByIdsAsync(IEnumerable<Guid>)` - Get securities by IDs
- `GetAllAsync()` - Get all securities
- `AddOrUpdateAsync(Security)` - Upsert security (finds by ticker, updates if exists, creates if not)
- `DeleteAsync(string)` - Delete security by ticker

**Key Logic**: Uses ticker as unique identifier for lookup, but stores Guid as primary key.

#### `IAllocationStrategyRepository` / `AllocationStrategyRepository`
- `GetTargetAllocationsByUserIdAsync(Guid)` - Get allocation strategies as Dictionary<string ticker, decimal percentage>
- `SetAllocationStrategyAsync(Guid, Guid, decimal)` - Set or update allocation strategy
- `GetDistinctCompanyIdsAsync()` - Get distinct SecurityIds that have allocation strategies

#### `IMarketPriceRepository` / `MarketPriceRepository`
- `GetCurrentPricesAsync(IEnumerable<string>)` - Get current market prices by tickers
- `UpdatePricesAsync(Dictionary<string, decimal>)` - Bulk update market prices

### Service Layer (Business Logic)

#### `ITransactionService` / `TransactionService`
- `Create(CreateTransactionRequest)` - Create transaction (validates security exists, sets UserId to RootUserId if null)
- `CreateBulk(List<CreateTransactionRequest>)` - Bulk create transactions
- `GetById(Guid)` - Get transaction by ID (returns PortfolioTransactionDto)
- `GetAllByUser(Guid?)` - Get all transactions for user (returns TransactionDto list)
- `Update(Guid, Guid, UpdateTransactionRequest)` - Update transaction (partial update, updates UpdatedAt)
- `Delete(Guid, Guid)` - Delete transaction

**Key Logic**:
- Validates security exists before creating transaction
- Sets `UpdatedAt = Date` on creation
- Sets `UpdatedAt = DateTime.UtcNow` on update
- Uses `Constants.User.RootUserId` if UserId is null

#### `ISecurityService` / `SecurityService`
- `GetAllAsync()` - Get all securities (returns CompanyDto list)
- `GetByTickerAsync(string)` - Get security by ticker (throws if not found)
- `CreateAsync(CreateCompanyRequest)` - Create security (sets LastUpdated)
- `UpdateAsync(string, UpdateCompanyRequest)` - Update security (updates SecurityType if provided, sets LastUpdated)
- `DeleteAsync(string)` - Delete security

#### `IPortfolioService` / `PortfolioService`
- `GetPortfolio(Guid?)` - Get user portfolio

**Key Logic**:
1. Gets open positions transactions for user
2. Groups by SecurityId
3. Fetches securities, market prices, and allocation strategies
4. Calculates total portfolio value using **total invested (cost basis)** - NOT market value
5. For each position:
   - Calculates total shares and average share price using weighted average cost basis
   - Calculates total invested (sum of TotalAmount from transactions)
   - Gets current market value (for display only)
   - Calculates current allocation using total invested
   - Gets target allocation if set
   - Calculates rebalancing amount and status
6. Orders positions by target allocation percentage (descending, nulls last)
7. Returns last 5 transactions per position (ordered by date descending)

**Important**: Rebalancing calculations use **total invested (cost basis)**, not current market value. This is intentional as market prices will be introduced in the future.

#### `IPortfolioInsightsService` / `PortfolioInsightsService`
- `GetInsights(Guid?)` - Get portfolio insights (rebalancing recommendations)

**Key Logic**:
- Filters positions where deviation > 0.5% (rebalancing threshold)
- Returns only positions that need rebalancing
- Uses total invested for calculations

#### `IAllocationStrategyService` / `AllocationStrategyService`
- `GetTargetAllocationsAsync(Guid)` - Get target allocations as Dictionary<string ticker, decimal percentage>
- `SetAllocationStrategyAsync(Guid, SetAllocationStrategyRequest)` - Set allocation strategy

#### `IMarketPriceService` / `MarketPriceService`
- `GetCurrentPricesAsync(IEnumerable<string>)` - Get current market prices by tickers
- Returns Dictionary<string ticker, decimal price>

### Calculation Service

#### `PortfolioCalculator` (Static Class)
Pure calculation service with no dependencies:

- `CalculatePositionMetrics(List<PortfolioTransactionDto>)` - Returns (totalShares, averageSharePrice, costBasis)
- `CalculateCostBasis(List<PortfolioTransactionDto>)` - Returns (totalShares, costBasis)
- `CalculateCurrentAllocationPercentage(decimal, decimal)` - Returns allocation %
- `CalculateRebalancingAmount(decimal, decimal, decimal)` - Returns rebalancing amount (positive = buy, negative = sell)
- `DetermineRebalancingStatus(decimal, decimal)` - Returns Balanced/Overweight/Underweight

**Key Algorithm**: **Weighted Average Cost Basis Method**
- Processes transactions chronologically (ordered by date ascending)
- Buy transactions: Add shares and cost (including fees)
- Sell transactions: Reduce shares and cost basis proportionally based on average cost per share
- Handles edge cases (selling more shares than owned, zero shares remaining)

**Rebalancing Threshold**: Currently `0.5%` (defined as constant `rebalancingThreshold`). Positions within ±0.5% are considered Balanced.

## 🚀 Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [PostgreSQL](https://www.postgresql.org/download/) (if not using Docker)

### Quick Start with Docker

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd babylon-api
   ```

2. **Start the services**
   ```bash
   cd src/Babylon.Alfred
   docker-compose up
   ```

3. **Access the API**
   - API: http://localhost:8000
   - Swagger UI: http://localhost:8000/swagger
   - Health Check: http://localhost:8000/health

### Manual Setup

1. **Set up the database**
   ```bash
   # Start PostgreSQL (if using Docker)
   docker run --name babylon-postgres \
     -e POSTGRES_DB=babylon_dev \
     -e POSTGRES_USER=postgres \
     -e POSTGRES_PASSWORD=postgres \
     -p 5432:5432 \
     -d postgres:16-alpine
   ```

2. **Update connection string**

   Edit `src/Babylon.Alfred/Babylon.Alfred.Api/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=babylon_dev;Username=postgres;Password=postgres"
     }
   }
   ```

3. **Apply migrations**
   ```bash
   cd src/Babylon.Alfred/Babylon.Alfred.Api
   dotnet ef database update
   ```

4. **Run the API**
   ```bash
   dotnet run
   ```

## 💻 Development

### Project Structure

```
babylon-api/
├── src/
│   └── Babylon.Alfred/
│       ├── Babylon.Alfred.Api/          # Main API project
│       │   ├── Features/                # Feature-based organization
│       │   │   ├── Investments/         # Investment management
│       │   │   │   ├── Controllers/
│       │   │   │   ├── Services/
│       │   │   │   └── Models/
│       │   │   ├── Telegram/           # Telegram bot integration
│       │   │   └── Startup/            # Health checks
│       │   └── Shared/                  # Shared components
│       │       ├── Data/                # DbContext, models, migrations
│       │       ├── Repositories/
│       │       ├── Middlewares/
│       │       └── Models/
│       ├── Babylon.Alfred.Api.Tests/    # Test project
│       ├── Babylon.Alfred.Worker/       # Background jobs
│       └── docker-compose.yml
└── iac/                                 # Infrastructure as Code
    └── components/
        └── babylon-api/                 # Terraform configuration
```

### Database Migrations

**Create a new migration:**
```bash
cd src/Babylon.Alfred/Babylon.Alfred.Api
dotnet ef migrations add MigrationName
```

**Apply migrations:**
```bash
dotnet ef database update
```

**Rollback migration:**
```bash
dotnet ef database update PreviousMigrationName
```

### Seed Data

The database includes a root user seeded automatically:
```csharp
RootUserId: "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"
```

You can seed companies using the SQL script:
```bash
psql -U postgres -d babylon_dev -f src/Babylon.Alfred/Babylon.Alfred.Api/Shared/Data/Scripts/SetupFreedomCompanies.sql
```

## 🧪 Testing

### Run All Tests

```bash
cd src/Babylon.Alfred
dotnet test
```

### Run Tests with Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Test Structure

```
Tests/
├── Features/
│   └── Investments/
│       ├── Controllers/
│       │   ├── SecuritiesControllerTests.cs
│       │   ├── TransactionsControllerTests.cs
│       │   └── PortfoliosControllerTests.cs
│       └── Services/
│           ├── SecurityServiceTests.cs
│           ├── TransactionServiceTests.cs
│           └── PortfolioServiceTests.cs
└── Shared/
    └── Repositories/
        ├── SecurityRepositoryTests.cs
        └── TransactionRepositoryTests.cs
```

### Test Conventions

#### Required Patterns
1. **AutoFixture** - Generate test data
2. **AutoMocker** (Moq.AutoMock) - Auto-mock dependencies
3. **FluentAssertions** - Fluent assertion syntax
4. **Guid.NewGuid()** - Use for creating Guids (NOT `fixture.Create<Guid>()`)

#### Test Setup Pattern
```csharp
public class ServiceTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly Service sut;

    public ServiceTests()
    {
        // Configure AutoFixture
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Customize DateOnly generation
        fixture.Customize<DateOnly>(composer => composer.FromFactory(() =>
            new DateOnly(2020, 1, 1)));

        sut = autoMocker.CreateInstance<Service>();
    }
}
```

#### Repository Tests
- Use **EF Core InMemory Database**
- Create `BabylonDbContext` with `UseInMemoryDatabase()`
- Test actual database operations
- Clean up in `Dispose()` method

#### Service Tests
- Mock repositories using AutoMocker
- Test business logic and validation
- Verify repository method calls

#### Controller Tests
- Mock services using AutoMocker
- Test HTTP responses and status codes
- Verify service method calls

### Test Naming Convention
`MethodName_Scenario_ExpectedBehavior`

Example: `GetAllByUser_WithUserId_ShouldReturnTransactionsOrderedByDateDescending`

## 🚀 Deployment

### Infrastructure

The project uses Terraform to manage AWS infrastructure:

```bash
cd iac/components/babylon-api
terraform init
terraform plan
terraform apply
```

**Resources created:**
- VPC with public subnets
- RDS PostgreSQL instance (db.t3.micro)
- Security groups
- Internet Gateway

### Environment Variables

For production deployment, set these environment variables:

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__DefaultConnection=<your-connection-string>
```

### Docker Build

```bash
cd src/Babylon.Alfred
docker build -t babylon-alfred-api -f Babylon.Alfred.Api/Dockerfile .
```

### Docker Run

```bash
docker run -d \
  -p 8000:8080 \
  -e ConnectionStrings__DefaultConnection="<connection-string>" \
  babylon-alfred-api
```

## 🔄 Worker Service (Background Jobs)

### Project: `Babylon.Alfred.Worker`
- **Type**: .NET Hosted Service
- **Purpose**: Scheduled background jobs
- **Scheduler**: Quartz.NET

### Current Jobs

#### `PriceFetchingJob`
- **Schedule**: Every minute (`0 * * * * ?`)
- **Purpose**: Fetch current market prices for securities
- **Service**: `PriceFetchingService`
- **Data Source**: `YahooFinanceService` (Yahoo Finance API)

**Logic**:
1. Gets distinct tickers from allocation strategies
2. Fetches current prices from Yahoo Finance
3. Updates `MarketPrice` table

### Worker Configuration
- Uses same `BabylonDbContext` as API
- Registers repositories and services
- Uses Serilog for logging
- Connection string from configuration

## 📝 Logging

### Serilog Configuration
- **Sinks**: Console, File
- **Configuration**: `appsettings.json`
- **Structured Logging**: Yes

### Logging Extensions (`LoggerExtensions.cs`)
Custom extension methods for consistent logging:
- `LogOperationStart(operation, context)` - Log operation start
- `LogOperationSuccess(operation, result, context)` - Log success
- `LogDatabaseOperation(operation, entityType, identifier, count)` - Log DB operations
- `LogApiRequest(method, path, userId, context)` - Log API requests
- `LogBusinessRuleViolation(operation, message, context)` - Log business rule violations
- `LogValidationFailure(operation, message, request)` - Log validation failures

### Middleware
- **RequestLoggingMiddleware**: Logs all HTTP requests
- **GlobalErrorHandlerMiddleware**: Catches exceptions, logs, and returns standardized error responses

## 🎨 Code Style & Conventions

### Naming Conventions
- **Controllers**: `*Controller` (e.g., `TransactionsController`)
- **Services**: `*Service` (e.g., `TransactionService`)
- **Repositories**: `*Repository` (e.g., `TransactionRepository`)
- **DTOs**: `*Dto`, `*Request`, `*Response` (e.g., `TransactionDto`, `CreateTransactionRequest`)
- **Models**: Domain entities use simple names (e.g., `Transaction`, `Security`)

### Method Naming
- **No "Async" suffix**: Service methods are async but don't use "Async" suffix (e.g., `GetAll()`, not `GetAllAsync()`)
- **Exception**: Repository methods use "Async" suffix (e.g., `GetAllAsync()`)

### File Organization
- **Feature-based**: Organized by feature, not by layer
- **Shared Code**: Common code in `Shared/` folder
- **Extensions**: Service registration in `Extensions/` folders

### Nullable Reference Types
- Enabled throughout the project
- Use `?` for nullable types
- Use `!` for null-forgiving operator when appropriate

## 📋 Business Rules

### Transaction Processing
1. **Security Validation**: Security must exist before creating transaction
2. **User Default**: If UserId is null, uses `Constants.User.RootUserId`
3. **UpdatedAt**: Set to transaction Date on creation, `DateTime.UtcNow` on update
4. **Ordering**: Transactions ordered by `UpdatedAt DESC`

### Portfolio Calculations
1. **Cost Basis Method**: Uses weighted average cost basis (not FIFO or LIFO)
2. **Rebalancing Basis**: Uses **total invested (cost basis)**, NOT current market value
3. **Open Positions**: Only includes positions with `totalShares > 0`
4. **Position Metrics**: Calculated chronologically (oldest transactions first)

### Allocation Strategy
1. **Uniqueness**: One allocation strategy per user per security
2. **Target Percentage**: 0-100 range
3. **Rebalancing Threshold**: ±0.5% deviation considered Balanced

### Security Management
1. **Ticker Uniqueness**: Ticker must be unique (enforced by unique index)
2. **Upsert Behavior**: `AddOrUpdateAsync` updates existing security if ticker exists
3. **SecurityType**: Required, defaults to `SecurityType.Stock`

### Key Constants

#### Constants (`Constants.cs`)
```csharp
public class Constants
{
    public struct User
    {
        public static readonly Guid RootUserId = Guid.Parse("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d");
    }
}
```

**Usage**: When `UserId` is null in requests, it defaults to `Constants.User.RootUserId`.

#### Rebalancing Threshold
- Defined in `PortfolioCalculator.DetermineRebalancingStatus()`
- Current value: `0.5m` (0.5 percentage points)
- Positions within ±0.5% of target are considered Balanced

#### Transaction Ordering
- **Default ordering**: `UpdatedAt DESC` (newest first)
- Used in: `GetAllByUser`, `GetOpenPositionsByUser`
- On creation: `UpdatedAt = Date`
- On update: `UpdatedAt = DateTime.UtcNow`

#### Portfolio Position Limits
- **Last N transactions**: Returns last 5 transactions per position (for display)
- **Ordering**: Positions ordered by `TargetAllocationPercentage DESC` (nulls last)

## 🗺️ Roadmap

### Planned Features
- [ ] Real-time stock price integration
- [ ] Portfolio performance metrics (gains/losses)
- [ ] Dividend tracking
- [ ] Complete Telegram bot integration
- [ ] Export to CSV/Excel
- [ ] Multi-currency support
- [ ] Transaction history filtering and search
- [ ] Dashboard UI (future separate project)

### Technical Improvements
- [ ] CI/CD pipeline setup
- [ ] Authentication & Authorization
- [ ] Application Insights integration
- [ ] API versioning
- [ ] Response caching
- [ ] Pagination for list endpoints

### Design Considerations
- **Ticker vs Exchange**: Currently uses single ticker per security. Future design may support multiple exchanges (see `TICKER_EXCHANGE_DESIGN.md`)
- **Market Prices**: Currently fetched from Yahoo Finance. May integrate multiple sources in future.
- **Rebalancing**: Currently uses cost basis. May switch to market value when market prices are fully integrated.

## 🤝 Contributing

This is a personal project, but contributions are welcome!

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Write tests for new features
- Keep methods small and focused
- Use AutoFixture, AutoMocker, FluentAssertions for tests
- Use `Guid.NewGuid()` for creating Guids in tests (not `fixture.Create<Guid>()`)

## 📚 Additional Resources

### Documentation Files
- `README.md` - This file (main project documentation)
- `COMPANY_TO_SECURITY_REFACTOR.md` - Refactoring notes (Company → Security)
- `TICKER_EXCHANGE_DESIGN.md` - Future design for multi-exchange support
- `LOGGING_GUIDELINES.md` - Logging best practices

### Database Scripts
- `Shared/Data/Scripts/SetupFreedomCompanies.sql` - Seed data script
- Migration files in `Shared/Data/Migrations/`

## 🎯 For AI Assistants

When designing new features or refactoring:

1. **Follow the architecture**: Use feature-based organization, repository pattern, service layer
2. **Use existing patterns**: Follow the same patterns for services, repositories, controllers
3. **Test everything**: Use AutoFixture, AutoMocker, FluentAssertions, and Guid.NewGuid()
4. **Respect business rules**: Cost basis calculations, rebalancing thresholds, transaction ordering
5. **Maintain consistency**: Naming conventions, nullable reference types, async patterns
6. **Consider multi-user**: Always support UserId (nullable, defaults to RootUserId)
7. **Use structured logging**: Use LoggerExtensions for consistent logging
8. **Handle errors**: Use GlobalErrorHandlerMiddleware pattern
9. **Update migrations**: Create EF Core migrations for schema changes
10. **Document changes**: Update this README if architecture or patterns change

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by the need for better personal finance tracking
- Named after Alfred Pennyworth, Batman's faithful butler
- Built with modern .NET practices and clean architecture principles

## 📧 Contact

For questions or suggestions, please open an issue on GitHub.

---

**Last Updated**: 2025-01-20
**Project Version**: .NET 9.0, EF Core 8.0
**Made with ❤️ using .NET 9.0**
