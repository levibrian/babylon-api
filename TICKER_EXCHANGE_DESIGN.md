# Ticker & Exchange Design Proposal

## Problem Statement
- Currently, `Company` has a single `Ticker` field
- User invests in both US and EU exchanges
- EU exchanges may use different ticker formats (e.g., "AAPL" vs "AAPL.XETR")
- Need to fetch market prices per exchange
- Need to track which exchange each transaction occurred on

## Proposed Solution: CompanyTicker Table

### Design Overview
Create a separate `CompanyTicker` table that allows multiple tickers per company, each associated with an exchange.

### Benefits
- ✅ **Flexible**: Supports unlimited exchanges per company
- ✅ **Normalized**: Proper database design
- ✅ **Future-proof**: Easy to add new exchanges
- ✅ **Price fetching**: Can fetch prices per exchange/ticker
- ✅ **Transaction tracking**: Can optionally track which exchange a transaction occurred on

### Database Schema

```csharp
public class CompanyTicker
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Ticker { get; set; } = string.Empty;  // e.g., "AAPL", "AAPL.XETR"
    public string Exchange { get; set; } = string.Empty;  // e.g., "NYSE", "XETR", "LSE"
    public bool IsPrimary { get; set; }  // Primary ticker for display
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Company Company { get; set; } = null!;
}

public class Company
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }

    // Navigation - keep for backward compatibility, but use CompanyTicker going forward
    public string Ticker { get; set; } = string.Empty;  // DEPRECATED: Use CompanyTicker instead

    // Navigation properties
    public ICollection<CompanyTicker> Tickers { get; set; } = new List<CompanyTicker>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<AllocationStrategy> AllocationStrategies { get; set; } = new List<AllocationStrategy>();
}

public class Transaction
{
    // ... existing fields ...
    public Guid CompanyId { get; set; }
    public Guid? CompanyTickerId { get; set; }  // OPTIONAL: Track which ticker/exchange was used

    // Navigation
    public Company Company { get; set; } = null!;
    public CompanyTicker? CompanyTicker { get; set; }  // Optional - for tracking exchange
}

public class MarketPrice
{
    public Guid Id { get; set; }
    public Guid CompanyTickerId { get; set; }  // Changed from string Ticker
    public decimal Price { get; set; }
    public DateTime LastUpdated { get; set; }

    // Navigation
    public CompanyTicker CompanyTicker { get; set; } = null!;
}
```

### Migration Strategy

**Phase 1: Add CompanyTicker table (non-breaking)**
1. Create `CompanyTicker` table
2. Migrate existing `Company.Ticker` values to `CompanyTicker` records (set `IsPrimary = true`, `Exchange = "US"`)
3. Keep `Company.Ticker` for backward compatibility

**Phase 2: Update MarketPrice (breaking)**
1. Add `CompanyTickerId` to `MarketPrice`
2. Migrate existing `MarketPrice` records to use `CompanyTickerId`
3. Remove `Ticker` string field from `MarketPrice`

**Phase 3: Optional - Track exchange in Transactions**
1. Add `CompanyTickerId` to `Transaction` (nullable)
2. Update transaction creation to optionally specify exchange

### API Changes

**Create Transaction:**
```json
POST /api/transactions
{
  "ticker": "AAPL",  // Still accepts ticker
  "exchange": "XETR",  // NEW: Optional exchange
  // ... other fields
}
```

**Get Portfolio:**
- Returns positions aggregated by `Company`
- Can optionally show exchange breakdown per position

**Market Price Fetching:**
- Worker fetches prices for all `CompanyTicker` records
- Uses `CompanyTicker.Ticker` + `CompanyTicker.Exchange` for API calls
- Stores prices with `CompanyTickerId`

### Alternative: Simpler Approach (Option 2)

If you prefer a simpler solution:

1. **Add `Exchange` enum to `Company`**:
   ```csharp
   public enum Exchange { US, EU, UK, /* ... */ }
   public Exchange Exchange { get; set; } = Exchange.US;
   ```

2. **Add `Exchange` to `Transaction`** (optional):
   ```csharp
   public Exchange? Exchange { get; set; }  // Track where transaction occurred
   ```

3. **Update market price fetching**:
   - Use `Company.Ticker` + `Company.Exchange` to construct API calls
   - Store prices with ticker + exchange combination

**Limitation**: Only one ticker per exchange per company. If AAPL trades on multiple EU exchanges, you'd need separate Company records.

---

## Recommendation

**Start with Option 2 (simpler)** if:
- You only need one ticker per exchange
- You want a quick solution
- You can refactor later if needed

**Go with Option 1 (CompanyTicker table)** if:
- You need multiple tickers per exchange
- You want the most flexible, normalized design
- You're willing to invest in the migration

Which approach do you prefer?

