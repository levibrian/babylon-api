``` markdown
# Babylon.Alfred.Api.Tests — Testing Contract

## Framework & Libraries

| Library | Purpose |
|---------|---------|
| xUnit | Test runner |
| AutoFixture | Test data generation (no manual object construction) |
| Moq.AutoMock (`AutoMocker`) | Automatic mock injection into SUT |
| FluentAssertions | Assertions — readable, precise |

## TDD Workflow

1. Read the business rule invariants in the relevant feature's CLAUDE.md.
2. Write the test — Red.
3. Implement minimum code to pass — Green.
4. Refactor without breaking tests.
5. If a new invariant is discovered, add it to the feature CLAUDE.md before writing the test.

## Test Class Structure
```

csharp public class FooServiceTests { private readonly Fixture fixture = new(); private readonly AutoMocker autoMocker = new(); private readonly FooService sut;
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

    // Act

    // Assert
}
}```

## Naming Convention
```

{MethodName}{Scenario}{ExpectedOutcome}
Examples: Create_WithValidRequest_ShouldReturnTransaction Create_WhenSecurityNotFound_ShouldThrowInvalidOperationException Delete_WhenBuyDeletedCausesFutureSellToOversell_ShouldThrow```

## Required Structure Per Test

Every test **must** have explicit `// Arrange`, `// Act`, `// Assert` comments, each separated by a blank line:
```

csharp [Fact] public async Task Create_WithValidRequest_ShouldReturnTransaction() { // Arrange var request = fixture.Build () .With(r => r.Amount, 100m) .Create(); autoMocker.GetMock () .Setup(x => x.GetByIdAsync(request.Id)) .ReturnsAsync(fixture.Create());
// Act
var result = await sut.Create(request);

// Assert
result.Should().NotBeNull();
result.Amount.Should().Be(100m);
}```

## Mocking Rules

| Always Mock | Never Mock |
|-------------|------------|
| Repositories (`I*Repository`) | Calculators (pure functions — test inputs/outputs directly) |
| External HTTP services | Validators (pure functions) |
| `IConfiguration` | Mappers (pure functions) |
| Any other `I*` infrastructure dependency | Value objects / DTOs |

- Use `autoMocker.GetMock<T>()` to configure mocks — never `new Mock<T>()` directly.
- Use `autoMocker.CreateInstance<T>()` to construct the SUT — never `new T(...)`.
- AutoFixture generates all test data — never hardcode magic values unless testing a specific boundary.

## AutoFixture Customisation Patterns

**DateOnly** (required — prevents invalid date generation):
```

csharp fixture.Customize(c => c.FromFactory(() => DateOnly.FromDateTime(DateTime.Today.AddDays(new Random().Next(-365, 365)))));```

**Recursive types** (always include in constructor):
```

csharp fixture.Behaviors.OfType() .ToList().ForEach(b => fixture.Behaviors.Remove(b)); fixture.Behaviors.Add(new OmitOnRecursionBehavior());```

**Constrained properties** (use `fixture.Build<T>().With(...).Create()`):
```

csharp var request = fixture.Build() .With(r => r.SharesQuantity, 10m) .With(r => r.Tax, 0m) .Create();```

## Coverage Expectations

- Every **service method** must have: happy path + primary failure path.
- Every **business rule invariant** listed in the feature's CLAUDE.md must map to at least one test.
- Every **threshold boundary** (e.g., concentration >20%, allocation deviation >0.5%) must be tested at, below, and above the boundary.
- Analyzers: all four (Risk, Income, Efficiency, Trend) must cover boundary conditions.
- Calculators: FIFO lots, splits, dividends, partial sells — all must have dedicated test cases.

## Test Folder Structure

Mirrors the source exactly:
```

Babylon.Alfred.Api.Tests/ ├── Features/ │ ├── Investments/ │ │ ├── Controllers/ │ │ ├── Services/ │ │ ├── Analyzers/ │ │ └── Shared/ # Calculator tests │ ├── Authentication/ │ └── RecurringSchedules/ ├── Infrastructure/ └── Shared/ └── Repositories/```

## Controller Tests

- Mock the service layer only (not repositories).
- Test: correct HTTP status codes, response shape (`ApiResponse<T>`), and that the service was called with correct arguments.
- Do **not** test business logic in controller tests.

## Service Tests

- Use `AutoMocker` to inject mocked repositories and dependencies.
- Test business logic, orchestration, and exception throwing.

## Calculator / Analyzer Tests

- No mocks. Pure input → output assertions.
- Use precise `decimal` values with `Should().Be(exactValue)`.
- FluentAssertions `.And` chaining is acceptable for related property assertions.
```
