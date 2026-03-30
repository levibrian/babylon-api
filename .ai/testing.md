# Testing — Babylon Alfred API

## 🚨 TDD IS MANDATORY. NO EXCEPTIONS. 🚨

**Before ANY code change:**
1. ✋ Write the test FIRST
2. 🔴 Verify it FAILS (Red)
3. ✅ Implement minimum code to PASS (Green)
4. ♻️ Refactor while keeping GREEN

**Tests are SPECIFICATION. Write them before production code.**

---

## Workflow

```
1. Read .ai/features/{feature}.md invariants
        ↓
2. Write failing test for new functionality
        ↓
3. Run → verify RED
        ↓
4. Write minimum production code
        ↓
5. Run → verify GREEN
        ↓
6. Refactor (keep green)
        ↓
7. Update .ai/features/{feature}.md with new invariants
```

---

## Framework & Libraries

| Library | Purpose |
|---------|---------|
| xUnit | Test runner (`[Fact]`, `[Theory]`, `[InlineData]`) |
| AutoFixture | Test data generation — no manual object construction |
| Moq.AutoMock (`AutoMocker`) | Automatic mock injection into SUT |
| FluentAssertions | Assertions — readable, precise (`Should()` syntax) |
| EF Core InMemory | Repository integration tests — unique DB per test |

---

## Test Class Structure

```csharp
public class FooServiceTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly FooService sut;

    public FooServiceTests()
    {
        // Always include recursion guard
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        sut = autoMocker.CreateInstance<FooService>();
    }
}
```

---

## Naming Convention

**Pattern**: `{MethodName}_{ScenarioCondition}_{ExpectedResult}`

**Examples**:
- `Create_WithValidRequest_ShouldReturnTransaction`
- `Create_WhenSecurityNotFound_ShouldThrowInvalidOperationException`
- `Delete_WhenBuyDeletedCausesFutureSellToOversell_ShouldThrow`
- `GetOpenPositions_WhenFullySold_ShouldExcludePosition`

---

## Required Structure Per Test

Every test **must** have explicit `// Arrange`, `// Act`, `// Assert` comments with blank line separators:

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

---

## Mocking Rules

| Always Mock | Never Mock |
|-------------|------------|
| Repositories (`I*Repository`) | Calculators (pure functions — test inputs/outputs directly) |
| External HTTP services | Validators (pure functions) |
| `IConfiguration` | Mappers (pure functions) |
| Any `I*` infrastructure dependency | Value objects / DTOs |

- Use `autoMocker.GetMock<T>()` to configure mocks — never `new Mock<T>()` directly
- Use `autoMocker.CreateInstance<T>()` to construct SUT — never `new T(...)`
- AutoFixture generates all test data — never hardcode magic values except at boundaries

---

## AutoFixture Customization Patterns

### DateOnly (always add to constructor)
```csharp
fixture.Customize<DateOnly>(c => c.FromFactory(() =>
    DateOnly.FromDateTime(DateTime.Today.AddDays(new Random().Next(-365, 365)))));
```

### GUID Generation
```csharp
var userId = Guid.NewGuid();        // CORRECT
var userId = fixture.Create<Guid>(); // WRONG
```

### Constrained Properties
```csharp
var request = fixture.Build<CreateTransactionRequest>()
    .With(r => r.SharesQuantity, 10m)
    .With(r => r.Tax, 0m)
    .Create();
```

---

## Coverage Requirements

- **Service methods**: Happy path + primary failure path minimum
- **Business rule invariants**: Every invariant in `.ai/features/{feature}.md` must have at least one test
- **Threshold boundaries**: Test at, below, AND above (e.g., concentration >20%, allocation deviation >0.5%)
- **Analyzers**: All four (Risk, Income, Efficiency, Trend) — all boundary conditions
- **Calculators**: FIFO lots, splits, dividends, partial sells — dedicated cases for each

### Mandatory Scenarios for Every Feature

1. Happy path — expected successful execution
2. Edge cases — boundary conditions, empty inputs, nulls
3. Error cases — all exceptions and validation failures
4. State changes — before/after assertions for mutations
5. Integration points — all external dependencies mocked and verified

---

## What to Test by Layer

### Controller Tests
Test **HTTP layer only**, not business logic:
- Correct HTTP status codes (200, 404, 400, 401, 500)
- Response shape (`ApiResponse<T>`)
- Service called with correct arguments
- User ID extracted from claims (`User.GetUserId()`)

```csharp
var controller = autoMocker.CreateInstance<PortfoliosController>();
controller.ControllerContext = CreateControllerContext(userId);
```

### Service Tests
Test business logic, orchestration, exception throwing:
- Happy path returns expected result
- Validation failures throw expected exceptions
- Business rules enforced (see feature invariants)
- Repository calls made with correct parameters

### Calculator / Analyzer Tests
Pure input → output, no mocks:
- Use precise `decimal` values: `Should().Be(exactValue)`
- FluentAssertions `.And` chaining acceptable for related properties

### Repository Tests (EF Core InMemory)
```csharp
var options = new DbContextOptionsBuilder<BabylonDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // unique per test
    .Options;
await using var context = new BabylonDbContext(options);
```

---

## Common Patterns

### Testing Exceptions
```csharp
Func<Task> act = async () => await sut.MethodName();
await act.Should().ThrowAsync<InvalidOperationException>()
    .WithMessage("*cannot sell more shares*"); // wildcard for partial match
```

### Testing Decimal Precision
```csharp
result.UnrealizedPnL.Should().BeApproximately(expected, 0.01m); // within 0.01
result.Percentage.Should().Be(25.5000m);                         // exact match
```

### Testing Async (never async void)
```csharp
[Fact]
public async Task TestMethod() { } // always Task, never void
```

---

## Invariants to Test (Cross-Reference)

| Category | Invariant | Source |
|----------|-----------|--------|
| Transaction | Selling more than held → `InvalidOperationException` | investments.md |
| Transaction | Dividend does NOT affect FIFO lots | investments.md |
| Transaction | Split multiplies shares in ALL lots | investments.md |
| Transaction | Tax only applies to Dividends | investments.md |
| Portfolio | Fully-sold positions excluded from open positions | investments.md |
| Portfolio | Positions ordered by TargetPercentage DESC, nulls last | investments.md |
| Allocation | Deviation < 0.5% → Balanced | investments.md |
| Allocation | Deviation ≥ 0.5% → Underweight/Overweight | investments.md |
| Risk | Concentration >20% → Warning | analyzers.md |
| Risk | Concentration >40% → Critical | analyzers.md |
| Auth | Duplicate email (with password) → `InvalidOperationException` | authentication.md |
| Auth | Expired/revoked refresh token → `UnauthorizedAccessException` | authentication.md |
| Recurring | Same (UserId, SecurityId) twice → updates existing | recurring-schedules.md |

---

## Anti-Patterns

```csharp
// BAD: Testing implementation details
mockRepo.Verify(x => x.SaveChanges(), Times.Once);

// BAD: Multiple unrelated assertions in one test — split into separate tests
result.Should().NotBeNull();
result.UserId.Should().Be(userId);
result.Transactions.Should().HaveCount(5);

// BAD: Hardcoded magic GUIDs
var userId = new Guid("12345678-1234-1234-1234-123456789012");

// BAD: Async void
public async void TestMethod() { }

// BAD: Testing framework code (that EF Core saves an entity)
```

---

## Test Project Structure

```
Babylon.Alfred.Api.Tests/
├── Features/
│   ├── Investments/
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Analyzers/    ← Risk, Income, Efficiency, Trend
│   │   └── Shared/       ← Calculator tests (Portfolio, RealizedPnL, Dividend, Statistics)
│   ├── Authentication/
│   │   ├── Controllers/
│   │   └── Services/
│   └── RecurringSchedules/
│       ├── Controllers/
│       └── Services/
├── Infrastructure/
│   └── YahooFinance/
└── Shared/
    └── Repositories/
```

---

## Running Tests

```bash
dotnet test                                              # all tests
dotnet test --filter "FullyQualifiedName~Investments"   # feature-specific
dotnet test /p:CollectCoverage=true                     # with coverage
```
