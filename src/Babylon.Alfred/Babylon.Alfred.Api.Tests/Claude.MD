# Babylon.Alfred.Api.Tests — Testing Contract

**Purpose**: Defines testing standards and conventions for the Babylon Alfred test suite. For general testing philosophy, see root `CLAUDE.md` § Testing Contract (TDD).

## Framework & Libraries

| Library | Purpose |
|---------|---------|
| xUnit | Test runner (`[Fact]`, `[Theory]`, `[InlineData]`) |
| AutoFixture | Test data generation (no manual object construction) |
| Moq.AutoMock (`AutoMocker`) | Automatic mock injection into SUT |
| FluentAssertions | Assertions — readable, precise (`Should()` syntax) |
| EF Core InMemory | Database tests with unique DB per test |

## TDD Workflow

1. Read the business rule invariants in the relevant feature's CLAUDE.md
2. Write the test → Red
3. Implement minimum code to pass → Green
4. Refactor without breaking tests
5. If a new invariant is discovered, add it to the feature CLAUDE.md before writing the test

## Test Class Structure

```csharp
public class FooServiceTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly FooService sut;

    public FooServiceTests()
    {
        // AutoFixture recursion guard (always include)
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        sut = autoMocker.CreateInstance<FooService>();
    }

    [Fact]
    public async Task MethodName_Scenario_ExpectedOutcome()
    {
        // Arrange
        var request = fixture.Create<SomeRequest>();

        // Act
        var result = await sut.MethodName(request);

        // Assert
        result.Should().NotBeNull();
    }
}
```

## Naming Convention

**Pattern**: `{MethodName}_{Scenario}_{ExpectedOutcome}`

**Examples**:
- `Create_WithValidRequest_ShouldReturnTransaction`
- `Create_WhenSecurityNotFound_ShouldThrowInvalidOperationException`
- `Delete_WhenBuyDeletedCausesFutureSellToOversell_ShouldThrow`
- `GetOpenPositions_WhenFullySold_ShouldExcludePosition`

## Required Structure Per Test

Every test **must** have explicit `// Arrange`, `// Act`, `// Assert` comments, each separated by a blank line:

```csharp
[Fact]
public async Task Create_WithValidRequest_ShouldReturnTransaction()
{
    // Arrange
    var request = fixture.Build<CreateTransactionRequest>()
        .With(r => r.Amount, 100m)
        .Create();
    autoMocker.GetMock<ITransactionRepository>()
        .Setup(x => x.GetByIdAsync(request.Id))
        .ReturnsAsync(fixture.Create<Transaction>());

    // Act
    var result = await sut.Create(request);

    // Assert
    result.Should().NotBeNull();
    result.Amount.Should().Be(100m);
}
```

## Mocking Rules

| Always Mock | Never Mock |
|-------------|------------|
| Repositories (`I*Repository`) | Calculators (pure functions — test inputs/outputs directly) |
| External HTTP services (`IHttpClientFactory`, etc.) | Validators (pure functions) |
| `IConfiguration` | Mappers (pure functions) |
| Any `I*` infrastructure dependency | Value objects / DTOs |

**Rules**:
- Use `autoMocker.GetMock<T>()` to configure mocks — never `new Mock<T>()` directly
- Use `autoMocker.CreateInstance<T>()` to construct the SUT — never `new T(...)`
- AutoFixture generates all test data — never hardcode magic values unless testing a specific boundary

## AutoFixture Customization Patterns

### DateOnly (Required)
Prevents invalid date generation:

```csharp
fixture.Customize<DateOnly>(c => c.FromFactory(() =>
    DateOnly.FromDateTime(DateTime.Today.AddDays(new Random().Next(-365, 365)))));
```

### Recursive Types (Always Include in Constructor)

```csharp
fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
    .ToList().ForEach(b => fixture.Behaviors.Remove(b));
fixture.Behaviors.Add(new OmitOnRecursionBehavior());
```

### Constrained Properties

Use `fixture.Build<T>().With(...).Create()`:

```csharp
var request = fixture.Build<CreateTransactionRequest>()
    .With(r => r.SharesQuantity, 10m)
    .With(r => r.Tax, 0m)
    .Create();
```

### GUID Generation

**Always** use `Guid.NewGuid()`, never `fixture.Create<Guid>()`:

```csharp
var userId = Guid.NewGuid(); // Correct
var userId = fixture.Create<Guid>(); // WRONG
```

## Coverage Expectations

- **Service methods**: Happy path + primary failure path minimum
- **Business rule invariants**: Every invariant in feature CLAUDE.md must have at least one test
- **Threshold boundaries**: Test at, below, and above (e.g., concentration >20%, allocation deviation >0.5%)
- **Analyzers**: All four (Risk, Income, Efficiency, Trend) must cover boundary conditions
- **Calculators**: FIFO lots, splits, dividends, partial sells — dedicated test cases for each

## Test Folder Structure

**Mirrors the API source exactly**:

```
Babylon.Alfred.Api.Tests/
├── Features/
│   ├── Investments/
│   │   ├── Controllers/         # Controller tests
│   │   ├── Services/            # Service tests
│   │   ├── Analyzers/           # Analyzer tests (Risk, Income, Efficiency, Trend)
│   │   └── Shared/              # Calculator tests (Portfolio, RealizedPnL, Dividend, Statistics)
│   ├── Authentication/
│   │   ├── Controllers/
│   │   └── Services/
│   └── RecurringSchedules/
│       ├── Controllers/
│       └── Services/
├── Infrastructure/
│   └── YahooFinance/            # Infrastructure service tests
└── Shared/
    └── Repositories/            # Repository tests (if any)
```

## Controller Tests

**Purpose**: Test HTTP layer only, not business logic.

**What to test**:
- Correct HTTP status codes (200, 404, 400, etc.)
- Response shape (`ApiResponse<T>`)
- Service was called with correct arguments
- User ID extraction from claims (`User.GetUserId()`)

**What NOT to test**:
- Business logic (that's the service layer's job)

**Example**:

```csharp
[Fact]
public async Task GetPortfolio_WithValidUserId_ShouldReturnOk()
{
    // Arrange
    var userId = Guid.NewGuid();
    var expectedResponse = fixture.Create<PortfolioResponse>();
    autoMocker.GetMock<IPortfolioService>()
        .Setup(x => x.GetPortfolioByUserId(userId))
        .ReturnsAsync(expectedResponse);

    var controller = autoMocker.CreateInstance<PortfoliosController>();
    controller.ControllerContext = CreateControllerContext(userId);

    // Act
    var result = await controller.GetPortfolio();

    // Assert
    var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
    var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PortfolioResponse>>().Subject;
    apiResponse.Success.Should().BeTrue();
    apiResponse.Data.Should().Be(expectedResponse);
}
```

## Service Tests

**Purpose**: Test business logic, orchestration, exception throwing.

**What to test**:
- Happy path returns expected result
- Validation failures throw expected exceptions
- Business rules are enforced (see feature CLAUDE.md for invariants)
- Repository calls are made with correct parameters

**Example**:

```csharp
[Fact]
public async Task Create_WhenSellingMoreThanHeld_ShouldThrowInvalidOperationException()
{
    // Arrange
    var userId = Guid.NewGuid();
    var securityId = Guid.NewGuid();
    var request = fixture.Build<CreateTransactionRequest>()
        .With(r => r.TransactionType, TransactionType.Sell)
        .With(r => r.SharesQuantity, 100m)
        .With(r => r.SecurityId, securityId)
        .Create();

    autoMocker.GetMock<ITransactionRepository>()
        .Setup(x => x.GetOpenPositionsByUser(userId))
        .ReturnsAsync(new List<Transaction>()); // No holdings

    // Act
    Func<Task> act = async () => await sut.Create(userId, request);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*cannot sell more shares than held*");
}
```

## Calculator / Analyzer Tests

**Purpose**: Test pure calculation logic (no side effects).

**Rules**:
- No mocks
- Pure input → output assertions
- Use precise `decimal` values with `Should().Be(exactValue)`
- FluentAssertions `.And` chaining is acceptable for related property assertions

**Example**:

```csharp
[Fact]
public void CalculateCostBasis_WithBuyAndFees_ShouldIncludeFeesNotTax()
{
    // Arrange
    var transaction = new Transaction
    {
        TransactionType = TransactionType.Buy,
        SharesQuantity = 10m,
        SharePrice = 100m,
        Fees = 5m,
        Tax = 2m // Should NOT be included in cost basis
    };

    // Act
    var costBasis = PortfolioCalculator.CalculateCostBasis(transaction);

    // Assert
    costBasis.Should().Be(1005m); // (10 * 100) + 5
}
```

## Database Tests (EF Core InMemory)

**When to use**: Integration tests for repositories or services that need DbContext.

**Pattern**: Unique database per test to prevent test pollution.

```csharp
[Fact]
public async Task GetOpenPositionsByUser_WhenFullySold_ShouldExclude()
{
    // Arrange
    var options = new DbContextOptionsBuilder<BabylonDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    await using var context = new BabylonDbContext(options);
    var userId = Guid.NewGuid();
    var securityId = Guid.NewGuid();

    // Add buy and sell that cancel out
    context.Transactions.AddRange(
        new Transaction { UserId = userId, SecurityId = securityId, TransactionType = TransactionType.Buy, SharesQuantity = 10m },
        new Transaction { UserId = userId, SecurityId = securityId, TransactionType = TransactionType.Sell, SharesQuantity = 10m }
    );
    await context.SaveChangesAsync();

    var repository = new TransactionRepository(context);

    // Act
    var positions = await repository.GetOpenPositionsByUser(userId);

    // Assert
    positions.Should().BeEmpty(); // Fully sold position excluded
}
```

## Test Data Generation Guidelines

**Use AutoFixture for**:
- Complex object graphs
- Lists and collections
- Any non-boundary value

**Use explicit values for**:
- Boundary conditions (e.g., 0, -1, exactly 20%, exactly 0.5%)
- Business rule thresholds
- Specific test scenarios (e.g., "selling 10 shares when holding 5")

**Example**:

```csharp
// Good: Testing boundary
var concentration = 0.20m; // Exactly at warning threshold

// Good: Testing specific scenario
var sharesHeld = 5m;
var sharesToSell = 10m;

// Good: Testing with generated data (non-boundary)
var transaction = fixture.Create<Transaction>();
```

## Common Test Patterns

### Testing Exception Messages

```csharp
await act.Should().ThrowAsync<InvalidOperationException>()
    .WithMessage("*cannot sell more shares*"); // Use wildcard for partial match
```

### Testing Collections

```csharp
result.Should().HaveCount(3);
result.Should().Contain(x => x.SecurityId == securityId);
result.Should().BeInDescendingOrder(x => x.TargetPercentage);
```

### Testing Decimals with Precision

```csharp
result.UnrealizedPnL.Should().BeApproximately(expected, 0.01m); // Within 0.01
result.Percentage.Should().Be(25.5000m); // Exact match
```

### Testing Async Methods

```csharp
// For methods that should throw
Func<Task> act = async () => await sut.MethodName();
await act.Should().ThrowAsync<ExceptionType>();

// For methods that should succeed
var result = await sut.MethodName();
result.Should().NotBeNull();
```

## Invariants to Test (Cross-Reference with Feature CLAUDE.md)

When writing tests for a feature, ensure these categories are covered:

1. **Transaction Rules** (Investments feature):
   - Selling more shares than held → throw
   - Dividend does NOT affect cost basis
   - Split multiplies shares in ALL lots
   - Tax only applies to Dividends

2. **Portfolio Position Rules** (Investments feature):
   - Fully-sold positions excluded from open positions
   - Positions ordered by TargetPercentage DESC, nulls last

3. **Allocation Rules** (Investments feature):
   - Deviation < 0.5% → Balanced
   - Deviation >= 0.5% → Underweight/Overweight

4. **Risk Rules** (Investments feature):
   - Concentration >20% → Warning
   - Concentration >40% → Critical

5. **Authentication Rules** (Authentication feature):
   - Duplicate registration → throw
   - Invalid Google token → throw
   - Expired refresh token → reject
   - Revoked token reuse → reject

6. **Upsert Rules** (RecurringSchedules feature):
   - Creating same (UserId, SecurityId) twice → updates existing

## Anti-Patterns (Do Not Do)

❌ **Testing implementation details**:
```csharp
// BAD: Testing internal method calls
mockRepo.Verify(x => x.SaveChanges(), Times.Once);
```

❌ **Multiple unrelated assertions**:
```csharp
// BAD: Testing too many things
result.Should().NotBeNull();
result.UserId.Should().Be(userId);
result.Transactions.Should().HaveCount(5);
result.CashBalance.Should().Be(1000);
// Split into multiple tests
```

❌ **Hardcoding magic GUIDs**:
```csharp
// BAD
var userId = new Guid("12345678-1234-1234-1234-123456789012");

// GOOD
var userId = Guid.NewGuid();
```

❌ **Testing framework code**:
```csharp
// BAD: Testing that EF Core works
[Fact]
public void Transaction_CanSetProperties()
{
    var transaction = new Transaction { Id = Guid.NewGuid() };
    transaction.Id.Should().NotBeEmpty();
}
```

❌ **Async void test methods**:
```csharp
// BAD
[Fact]
public async void TestMethod() { } // Should return Task

// GOOD
[Fact]
public async Task TestMethod() { }
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests for specific feature
dotnet test --filter "FullyQualifiedName~Investments"

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

## References

- **Root CLAUDE.md § Testing Contract (TDD)**: High-level testing philosophy
- **Feature CLAUDE.md files**: Business rule invariants that must be tested
- **xUnit documentation**: https://xunit.net/
- **FluentAssertions documentation**: https://fluentassertions.com/
