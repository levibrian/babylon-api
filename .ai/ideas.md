# Ideas — parking lot

Not in scope. To build any of these, change the north star first.

- **Analyzers** (Risk/Income/Efficiency/Trend)
- **Telegram bot** (scaffolded stub)
- **Trade Republic live sync**: spec removed from the tree; recover with `git show f548249:src/Babylon.Alfred/Babylon.Alfred.Api/Features/BrokerIntegration/CLAUDE.MD`
- **DB migration AWS RDS → Fly Postgres**: spike; target config in `fly.db.prod.toml`. Includes hardening: rotated secrets via `fly secrets`, private networking, encryption at rest.
- **Smart / timed rebalancing**: lives in `babylon-app`, beyond € buy/sell per position
- **Buy-only rebalancing via next contribution**
- **Publish the MCP / babylon as a product** (bebabylon). Note: profiling that leads to a recommended strategy is a MiFID II suitability assessment, i.e. regulated advice, once offered to others.
- **babylon-app frontend**
- **Two-level targets (buckets)**: bucket % per asset type (`SecurityType`), then % per security within the bucket. Bucket = asset type, not platform. Target model (stored vs derived buckets) undecided.
