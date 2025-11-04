# Babylon Alfred API 🏦

A personal investment tracking API built with ASP.NET Core 8.0 that helps manage and monitor stock portfolio transactions. Named after Batman's loyal butler, Alfred, this API serves as your automated assistant for investment management.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## 📋 Table of Contents

- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Getting Started](#-getting-started)
- [Development](#-development)
- [API Endpoints](#-api-endpoints)
- [Testing](#-testing)
- [Deployment](#-deployment)
- [Project Structure](#-project-structure)
- [Contributing](#-contributing)

## ✨ Features

### Investment Management
- 📊 **Portfolio Tracking**: View aggregated portfolio positions with real-time calculations
- 💰 **Transaction Management**: Record buy/sell transactions with detailed pricing and fees
- 🏢 **Company Database**: Maintain metadata for tracked companies
- 👤 **Multi-User Support**: Track investments for multiple users

### Technical Features
- 🔄 **Bulk Operations**: Import multiple transactions at once
- 📈 **Portfolio Analytics**: Calculate total invested, position sizes, and transaction history
- 🤖 **Telegram Bot Integration**: (Work in progress) Manage investments via Telegram
- 🏥 **Health Checks**: Monitor API availability
- 📝 **Swagger Documentation**: Interactive API documentation

## 🛠 Tech Stack

### Backend
- **Framework**: ASP.NET Core 8.0
- **Language**: C# 12 with nullable reference types
- **Database**: PostgreSQL 16+ with Entity Framework Core
- **ORM**: Entity Framework Core 8.0
- **Serialization**: Newtonsoft.Json with custom converters

### Infrastructure
- **Cloud**: AWS (RDS, VPC)
- **IaC**: Terraform
- **Containerization**: Docker & Docker Compose
- **CI/CD**: (To be implemented)

### Testing
- **Framework**: xUnit
- **Mocking**: Moq with AutoMock
- **Assertions**: FluentAssertions
- **Test Data**: AutoFixture
- **Database Testing**: EF Core InMemory Provider

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
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

## 📡 API Endpoints

### Companies

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/companies` | Get all companies |
| GET | `/api/v1/companies/{ticker}` | Get company by ticker |
| POST | `/api/v1/companies` | Create a new company |
| PUT | `/api/v1/companies/{ticker}` | Update company |
| DELETE | `/api/v1/companies/{ticker}` | Delete company |

### Transactions

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/transactions` | Create a transaction |
| POST | `/api/v1/transactions/bulk` | Create multiple transactions |

### Portfolio

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/portfolios/{userId}` | Get user portfolio |

### Example Requests

**Create a company:**
```bash
curl -X POST http://localhost:8000/api/v1/companies \
  -H "Content-Type: application/json" \
  -d '{
    "ticker": "AAPL",
    "companyName": "Apple Inc."
  }'
```

**Create a transaction:**
```bash
curl -X POST http://localhost:8000/api/v1/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "ticker": "AAPL",
    "transactionType": "Buy",
    "date": "2025-01-15",
    "sharesQuantity": 10,
    "sharePrice": 150.00,
    "fees": 5.00,
    "userId": null
  }'
```

**Get portfolio:**
```bash
curl http://localhost:8000/api/v1/portfolios/{userId}
```

For detailed API documentation, visit the Swagger UI at `/swagger` when the API is running.

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
│       │   ├── CompaniesControllerTests.cs
│       │   ├── TransactionsControllerTests.cs
│       │   └── PortfoliosControllerTests.cs
│       └── Services/
│           ├── CompanyServiceTests.cs
│           ├── TransactionServiceTests.cs
│           └── PortfolioServiceTests.cs
└── Shared/
    └── Repositories/
        ├── CompanyRepositoryTests.cs
        └── TransactionRepositoryTests.cs
```

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

## 📊 Architecture

### Design Patterns

- **Vertical Slice Architecture**: Features organized by business capability
- **Repository Pattern**: Data access abstraction
- **Dependency Injection**: Built-in ASP.NET Core DI
- **Global Error Handling**: Middleware-based exception handling

### Key Concepts

- **Companies**: Reference data for stocks/securities
- **Transactions**: Individual buy/sell operations
- **Portfolio**: Aggregated view of open positions
- **Users**: Multi-user support with nullable UserId

### Database Schema

```
┌─────────────┐       ┌──────────────┐
│   Users     │       │  Companies   │
├─────────────┤       ├──────────────┤
│ Id (PK)     │       │ Ticker (PK)  │
│ ...         │       │ CompanyName  │
└─────────────┘       │ LastUpdated  │
       │              └──────────────┘
       │
       │              ┌──────────────────────┐
       └──────────────│   Transactions       │
                      ├──────────────────────┤
                      │ Id (PK)              │
                      │ Ticker               │
                      │ TransactionType      │
                      │ Date                 │
                      │ SharesQuantity       │
                      │ SharePrice           │
                      │ Fees                 │
                      │ UserId (FK)          │
                      └──────────────────────┘
```

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
- [ ] Logging with Serilog
- [ ] Application Insights integration
- [ ] API versioning
- [ ] Response caching
- [ ] Pagination for list endpoints

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

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by the need for better personal finance tracking
- Named after Alfred Pennyworth, Batman's faithful butler
- Built with modern .NET practices and clean architecture principles

## 📧 Contact

For questions or suggestions, please open an issue on GitHub.

---

**Made with ❤️ using .NET 8.0**

