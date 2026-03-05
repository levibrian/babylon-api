CLAUDE.md Improvement Plan — Spec-Driven Design Audit
Goal: Maximise signal-to-token ratio. Every byte an AI reads must earn its cost by reducing ambiguity, preventing wrong decisions, or accelerating TDD tasks. Below is the analysis and concrete recommendations per level.

1. Cross-Cutting Problems (Affects All Files)
   1.1 No TDD Contract Defined Anywhere
   Every file is silent on how tests should be written. Given TDD is a project-wide commitment, this needs to be a first-class citizen.
   Fix: Add a root-level ## Testing Contract block to the root CLAUDE.md (currently the API-level one) and reference it in each child via a single-line pointer. Do not duplicate it.
   1.2 Directory Trees Are Expensive to Maintain and Rarely Necessary
   ASCII trees are high-token, low-value once the AI can read the actual filesystem. They drift out of sync with reality and mislead the model.
   Fix: Replace trees with structural principles ("feature folders contain Controllers/, Services/, Models/, Extensions/") and a single "anchor file" list (only truly non-obvious files). Remove trees that just mirror the filesystem.
   1.3 No Explicit "What NOT to Touch" Signals
   Every file describes what exists but never flags immutability constraints or dangerous coupling points. The AI may confidently suggest changes that break cross-service contracts.
   Fix: Add a ## Invariants / Do Not Change Without Discussion block to any file with significant cross-boundary coupling (Data, Repositories, Auth, Worker).
   1.4 Duplication of DI Lifetime Rules
   "Scoped" registration is documented in the root CLAUDE.md, Startup CLAUDE.md, and Repositories CLAUDE.md — three times. Tokens wasted.
   Fix: State DI rules once in root. Child files say DI: see root CLAUDE.md unless they override the default.
   1.5 No Token Budget Guidance (Context Loading Strategy)
   There is no guidance for the AI on which CLAUDE.md files to load for which tasks. The AI can't self-navigate the hierarchy.
   Fix: Add a ## Context Loading Guide to root that says:
   Modifying a service? Load: root + that feature's CLAUDE.md + Repositories CLAUDE.md. Adding a new feature? Load: root + Startup CLAUDE.md + Data CLAUDE.md + feature CLAUDE.md. Writing tests? Load: root (Testing Contract) + target feature CLAUDE.md.
2. Root / API-Level CLAUDE.md
   Current Issues
   The "Project Structure" tree is 30+ lines; most of it is already in child CLAUDE.md files.
   "Adding a New Feature" steps 1-4 are implementation steps, not spec. The AI shouldn't need a walkthrough — it needs the rule.
   "Middleware pipeline" ordering has business impact but no rationale (e.g., "CORS must be first for preflight"). Some rationale exists for CORS but nothing else.
   appsettings.json key listing is duplicated in Worker CLAUDE.md and could drift.
   No mention of what the test project (Babylon.Alfred.Api.Tests) is called or its structure convention.
   Recommended Changes
   Section
   Action
   Project Structure tree
   Replace with: "Vertical slices under Features/, cross-cutting in Shared/, external adapters in Infrastructure/. Each feature is self-contained."
   Startup / Middleware ordering
   Keep, but add reason for each step
   "Adding a New Feature"
   Convert to a ## Feature Checklist with ✅ items (spec, not tutorial)
   Configuration
   Remove duplication; link to appsettings.json directly
   New section: Testing Contract
   Add (see §5 below)
   New section: Context Loading Guide
   Add (see §1.5 above)
   New section: Global DOs and DON'Ts
   Consolidate the scattered convention notes

3. Feature-Level CLAUDE.md Files
   3.1 Investments — Mostly Good, Needs Trimming
   Strengths: FIFO algorithm, constants, analyzer pattern — excellent context signals.
   Issues:
   The architecture tree (60+ lines) is redundant if the AI has filesystem access.
   "Testing" section just says what exists, not how or what invariants tests must verify.
   No explicit business rule enumeration that an AI can use for test case generation.
   No "Edge Cases / Known Traps" section (e.g., dividends don't affect cost basis; fully-sold positions are excluded from portfolio view).
   Recommendations:```
## Business Rule Invariants (TDD Anchors)
- Selling more shares than held → must throw (tested in TransactionServiceTests)
- Dividend → does NOT affect FIFO lots or cost basis
- Split → multiplies shares in ALL open lots, price = 0
- Fully-sold positions → excluded from GetOpenPositionsByUser
- TargetAllocation deviations < 0.5% → treated as Balanced (no action)
```

Remove the architecture tree. Keep service/analyzer inventory as a table (much cheaper than tree).
3.2 Authentication — Good, But Missing Test Anchors
Issues:
Security Notes is valuable but buried at bottom. Should be elevated.
No mention of what tests exist or what invariants they cover.
"Supports account linking" is mentioned but has no spec detail — the AI may implement it inconsistently.
Recommendations:
Promote Security Notes to top as ## Security Invariants.
Add ## Test Anchors listing scenarios that must have test coverage: duplicate registration, invalid Google token, expired refresh token, revoked token reuse.
Specify account linking rule precisely: "If email matches existing local user, link Google provider. Username not changed."
3.3 RecurringSchedules — Over-Specified, Under-Specified
Issues:
Lists files that are obvious from folder conventions.
Says "upsert semantics" but doesn't define the upsert key (UserId + SecurityId? just Id?).
No mention of tests (no CLAUDE.md in test folder for this feature either).
Recommendations:
Remove file listing (covered by project conventions).
State upsert key explicitly.
Note test coverage expectation: "Service tests must cover upsert idempotency."
3.4 Telegram — Incomplete Feature, Should Be Minimised
Issues:
Wastes tokens describing a scaffolded feature that has no business logic yet.
Any AI asked to work in this area will load this and get very little useful context.
Recommendations:
Reduce to 5 lines max: status, what's wired, what's NOT wired, and a "do not implement logic here without a separate spec."
Add a ## Spec Required block with a checklist of what needs to be defined before implementation.
3.5 Startup — Fine, Minor Issues
"RegisterFeatures()" description is good but doesn't mention the order of registration (which matters if there are dependencies between feature registrations).
Health check note about Fly.io is environment-specific detail that could become stale.

4. Infrastructure CLAUDE.md
Issues:
Final paragraph about "HTTP Client Configuration" contradicts itself: says the API project uses the same models but Worker handles HTTP calls, then says clients are registered in the Worker's ServiceCollectionExtensions. Confusing.
"Adding a New External Integration" steps are tutorial-style, not spec.
No mention of mocking strategy for tests (are Yahoo Finance services abstracted behind interfaces in tests?).
Recommendations:
Replace "Adding" steps with a ## Integration Contract rule: "Every external integration must expose an interface consumed by feature services. The implementation is injected — never instantiated directly."
Clarify the API vs Worker split explicitly in one sentence.
Add ## Test Strategy: "Infrastructure services are tested via integration tests or with HttpClient mock (no actual HTTP calls in unit tests)."

5. Shared Layer CLAUDE.md Files
5.1 Data — Excellent, Needs One Addition
This is the best CLAUDE.md in the project. The precision table and entity relationship diagram are high-value, low-overhead context.
One gap: No migration rule. AI may generate schema-breaking migrations.
Add:``` markdown
## Migration Rules
- NEVER drop columns; add new nullable columns and migrate data separately.
- NEVER rename columns in a single migration (add + backfill + drop separately).
- All migrations must be reviewed: run `dotnet ef migrations script` and inspect SQL before applying.
- Test migrations against a local PostgreSQL instance before committing.
```

5.2 Repositories — Good, One Clarification Needed
GetOpenPositionsByUser is called out but there's no definition of "open position" (does it mean any non-zero net share count?). The AI could implement this wrong.
Add: Explicit definition: "Open position = security where SUM(Buy shares) - SUM(Sell shares) > 0 for a given user."

6. Worker CLAUDE.md
   Issues:
   Quartz cron expressions documented but no rationale for the 15-minute offset on snapshot job.
   RealizedGainsBackfillService is described as "one-time startup" but it's wired as BackgroundService — will it re-run on every redeploy? This is a hidden trap.
   No test section at all.
   JSON config snippet adds minimal value (AI can read appsettings.json).
   Recommendations:
   Add rationale for schedule offsets.
   Flag the backfill service as a potential idempotency concern with a warning: "Backfill checks if value changed before writing. Safe to re-run. Consider removing once all rows are populated."
   Add ## Test Strategy: "Jobs are not directly unit tested. Services (PriceFetchingService, PortfolioSnapshotService) are tested in isolation via mocked repositories."
   Remove JSON config block.

7. Missing CLAUDE.md Files
   Location
   Why It's Needed
   Babylon.Alfred.Api.Tests/CLAUDE.md
   Defines test conventions: framework (xUnit?), mocking library (Moq/AutoMocker?), naming pattern, Arrange/Act/Assert structure, what gets mocked vs what doesn't.
   Babylon.Alfred.Api/Shared/CLAUDE.md
   Context loading shortcut: "Shared contains Data, Repositories, Middlewares, Logging, Models, Extensions. Do not add feature-specific logic here."
   Babylon.Alfred.Api/Features/Investments/Analyzers/CLAUDE.md
   Analyzer contract is non-trivial. The strategy pattern interface, input/output expectations, and threshold rules deserve their own spec.

8. Proposed Global DO/DON'T Block (for Root CLAUDE.md)
   This consolidates scattered conventions from across all files:``` markdown
## Global Rules

### ✅ DOs
- One controller per aggregate (thin, delegate all logic to services).
- Primary constructor injection everywhere (C# 12).
- Return `ApiResponse<T>` for all success responses.
- Use `LoggerExtensions` methods (never raw `logger.LogX()`).
- All repository methods: async, `Async` suffix.
- Use `User.GetUserId()` to extract the authenticated user ID.
- Register everything as Scoped unless there's a documented reason not to.

### ❌ DON'Ts
- No business logic in controllers.
- No business logic in repositories.
- No repository-to-repository calls.
- No `Async` suffix on service method names.
- No direct `DbContext` use outside of repositories.
- No `Controller` base class (use `ControllerBase`).
- No `DateTime.Now` — always `DateTime.UtcNow`.
```


9. Testing Contract Block (for Root CLAUDE.md)``` markdown
## Testing Contract (TDD)

### Framework & Libraries
- xUnit + Moq + Moq.AutoMocker
- FluentAssertions for assertions

### Conventions
- Test class: `{SubjectClass}Tests` in mirrored folder under `Babylon.Alfred.Api.Tests/`.
- Method naming: `{Method}_{Scenario}_{ExpectedOutcome}` (e.g., `AddTransaction_WhenSellingMoreThanHeld_ThrowsException`).
- Structure: Arrange / Act / Assert with blank line separators.
- One logical assertion per test (FluentAssertions `.And` chaining is acceptable).

### What to Mock
- Always mock: repositories, external HTTP services, `IConfiguration`.
- Never mock: calculators, validators, mappers (these are pure functions — test directly).

### Coverage Expectations
- Every service method has at least: happy path + primary failure path.
- Every business rule invariant (documented in feature CLAUDE.md) must have a corresponding test.
- Analyzers: every threshold boundary must be tested (at, below, above).

### TDD Workflow
1. Write test based on business rule in CLAUDE.md.
2. Run → Red.
3. Implement minimum code to pass.
4. Refactor.
5. Update CLAUDE.md if new invariant discovered.
```


Summary Priority Matrix
Priority
Change
Impact
Cost
🔴 High
Add Testing Contract to root
Every AI task involving tests is currently underspecified
Low
🔴 High
Add Context Loading Guide to root
Prevents loading all CLAUDE.md files for every task
Low
🔴 High
Add Business Rule Invariants to Investments CLAUDE.md
Highest-value feature, most complex business logic
Low
🟠 Medium
Add Migration Rules to Data CLAUDE.md
Prevents irreversible schema damage
Low
🟠 Medium
Remove/slim ASCII trees from all files
~25% token reduction across all files
Medium
🟠 Medium
Add Tests CLAUDE.md for test project
Defines the missing testing spec layer
Medium
🟡 Low
Consolidate DI lifetime rules to root only
Small token saving
Low
🟡 Low
Slim Telegram CLAUDE.md
Minimal ROI feature consuming full spec tokens
Low
🟡 Low
Clarify Worker backfill idempotency
Prevents accidental data corruption
Low
