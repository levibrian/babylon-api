# Login/consent page redesign

**Status: Done (2026-10-27).** TDD throughout; 413/415 unit tests pass (2 pre-existing skips), 7/7 integration tests pass, full `dotnet build` green.

## Implementation notes
- Code changes matched the plan table exactly, with one addition: `CompleteGoogleSignIn` now calls `authorizationStore.SavePending(pending with { UserId, UserEmail })` to mint a *new* request id for the consent step (not the same id), consistent with how the sign-in step already worked.
- Accent color actually vendored from `@babylon/tokens/dist/tokens.css` is `#7b2fbe` / hover `#9d4edd` — the plan doc's `#7c3aed` was an approximation; the real token file is the source of truth and is what got copied.
- New integration test (`Babylon.Alfred.Api.IntegrationTests/Mcp/AuthorizePageStaticFilesTests.cs` + `McpApiFactory.cs`) required two things not anticipated in the plan, because this is the first integration test to boot the **Mcp** host rather than the Api host:
  - `Babylon.Alfred.Api.IntegrationTests.csproj` needed a new `ProjectReference` to `Babylon.Alfred.Mcp.csproj` with `Aliases="McpAssembly"` — both Api's and Mcp's top-level `Program.cs` generate a type named `Program` in the global namespace, so referencing both assemblies unqualified in the same project is ambiguous. Fixed with `extern alias McpAssembly;` + `McpAssembly::Program` in the new factory.
  - `Babylon.Alfred.Mcp.csproj` needed `<InternalsVisibleTo Include="Babylon.Alfred.Api.IntegrationTests" />` since `Program` is internal by default; `WebApplicationFactory<Program>` as a base class still enforces accessibility symmetry even with InternalsVisibleTo, so `McpApiFactory` and its test class are `internal` (xUnit discovers internal test classes fine within the test assembly itself — the "test classes must be public" rule only applies for cross-assembly discovery).
- No Dockerfile change needed: the Mcp Dockerfile already does `COPY src/Babylon.Alfred/Babylon.Alfred.Mcp/ ...` wholesale, which picks up `wwwroot/` automatically, and the csproj's existing exclusion targets only touch the Api's `appsettings*.json`, not static files.

Visual + UX rework of the MCP `/authorize` flow: branded dark UI matching `bebabylon-ui`, plus a missing consent step. Related: [[mcp-auth-spike]], [[auth-ui-shared-component]].

## Why

The current page (`AuthorizePage.cs`) is unstyled system-ui HTML — functional but generic, and jumps straight from Google sign-in to redirect with no consent step. This closes gap 5 from `mcp-auth-spike.md` ("no consent screen beyond the Google page") and gives the OAuth flow a professional look before more clients (claude.ai, ChatGPT, future first-party apps) rely on it.

Scope: visual redesign + consent step only. No scopes system, no revocation endpoint, no CORS — those stay open gaps per the spike doc.

## Design decisions

- **Dark only**, no `prefers-color-scheme` — consistent with bebabylon.com, not theme-aware.
- **Consent screen with concrete permission bullets**, not a generic "wants to access your portfolio" line. Static text (no real scopes exist yet):
  - Ver tus posiciones, transacciones y perfil de inversor
  - Modificar transacciones, cash balance y allocation targets
  - Importar movimientos desde Trade Republic
- **Assets as static files in `wwwroot`** (not inline-in-C#, not an npm build step). CSS vendored by hand-copying the relevant variables from `@babylon/tokens`' `dist/tokens.css` (dark-mode block + typography/spacing/radius) into a local stylesheet; logo copied from `bebabylon-ui/public/babylon_logo.png`. Accepted drift risk: if `@babylon/tokens` changes, this page needs a manual re-copy — fine for a page that changes rarely, avoids pulling Node tooling into a .NET service for one screen.
- **Brand tokens used:** `--color-bg-base: #080808`, `--color-bg-surface: #101010`, `--color-text-primary: #ffffff`, `--color-text-secondary: #a3a3a3`, `--color-accent: #7c3aed` / hover `#9d4edd`, `--color-border: rgba(255,255,255,0.08)`, font `Inter` (sans) + `JetBrains Mono` (mono, for client name / redirect host), radius `--radius-lg: 10px`, shadow `--shadow-modal`.

## Flow change

Today: `GET /authorize` → sign-in page → `POST /authorize/google` → Google credential validated → code issued → redirect to client. Two steps, no explicit consent.

New: `GET /authorize` → sign-in page → `POST /authorize/google` → Google credential validated → **consent page** (shows signed-in email, client name, permission bullets, Authorize/Cancel) → `POST /authorize/consent` → `decision=authorize` issues code and redirects; `decision=deny` redirects with `access_denied`. Three steps.

## Code changes

| File | Change |
|---|---|
| `Models/AuthorizationGrant.cs` | `PendingAuthorization` gains `Guid? UserId` and `string? UserEmail`, both null until Google sign-in completes. |
| `Models/AuthorizeOutcome.cs` | New case `ShowConsent(string RequestId, string ClientName, string UserEmail, string RedirectHost)`. |
| `Services/McpOAuthService.cs` | `CompleteGoogleSignIn`: after `AuthenticateGoogleUserAsync` succeeds, stop issuing the code. Instead re-save the pending via `authorizationStore.SavePending(pending with { UserId = user.Id, UserEmail = user.Email })` (new `request_id`) and return `ShowConsent`. New method `CompleteConsent(string? requestId, string decision)`: `TakePending`, if `decision == "authorize"` issue code + `RedirectTo` (the logic that used to sit at the end of `CompleteGoogleSignIn`); if `decision == "deny"` → `RedirectWithError(pending.RedirectUri, pending.State, "access_denied")`; any other value or an expired/missing pending → `ShowError`. |
| `Controllers/OAuthController.cs` | New endpoint: `[HttpPost("/authorize/consent")] [Consumes("application/x-www-form-urlencoded")] AuthorizeConsent([FromForm(Name = "request_id")] string? requestId, [FromForm(Name = "decision")] string decision)` → `Render(await oauthService.CompleteConsent(requestId, decision))`. `Render` switch gets a new arm for `ShowConsent` → `Html(AuthorizePage.Consent(consent.ClientName, consent.UserEmail, consent.RedirectHost, consent.RequestId))`. |
| `Services/AuthorizePage.cs` | Rewrite `Layout`, `SignIn`, add `Consent`. Dark theme, logo, Inter font, branded Google button container, consent bullets, two submit buttons (`Authorize` primary / `Cancel` secondary) in one form posting to `/authorize/consent` with hidden `request_id` and the clicked button's `decision` value. |
| `wwwroot/styles/authorize.css` (new) | Vendored subset of `@babylon/tokens` dark palette + typography/spacing/radius variables, plus layout rules for the sign-in/consent card. |
| `wwwroot/babylon_logo.png` (new) | Copied from `bebabylon-ui/public/babylon_logo.png`. |
| `Program.cs` | Add `app.UseStaticFiles()`. |
| `Babylon.Alfred.Mcp.csproj` | Confirm `wwwroot` is included in publish output (default for ASP.NET Core templates, but this project excludes some content already — see `mcp-auth-spike.md`'s note about excluding the Api's `appsettings*.json`, so verify nothing else is excluding `wwwroot`). |

## Testing

Extend `Babylon.Alfred.Api.Tests/Mcp/McpOAuthServiceTests.cs`:
- `CompleteGoogleSignIn` with a valid credential now returns `ShowConsent`, not `RedirectTo` — update the existing happy-path test's assertion.
- `CompleteConsent` with `decision=authorize` on a valid pending (with `UserId` set) issues a redeemable code and returns `RedirectTo` with `code`/`state`/`iss`.
- `CompleteConsent` with `decision=deny` returns `RedirectTo` with `error=access_denied`.
- `CompleteConsent` with an expired or unknown `request_id` returns `ShowError`.
- `CompleteConsent` with an unrecognized `decision` value returns `ShowError`.
- New `WebApplicationFactory` test: `GET /babylon_logo.png` (served from `wwwroot` at the site root, ASP.NET Core default) returns 200 with an image content type, confirming static files are wired up.

## Out of scope (explicitly deferred)

- Moving this UI into `babylon-app` as a reusable component — tracked separately in `auth-ui-shared-component.md`, happens only when the `babylon-app` rewrite starts.
- Real OAuth scopes, a revocation endpoint, CORS, rate limiting on `/register`/`/token` — all still open per `mcp-auth-spike.md`'s gap list, unaffected by this change.
