# MCP auth spike (step 6)

**Result: build it in the API.** A minimal authorization server works locally against the spec. It took well under the "few days" budget, and what's left is small. No hosted provider needed.

Branch `spike/mcp-oauth`. Code lives in `Features/Mcp/`.

## What works (locally, covered by tests)

| Piece | How |
|---|---|
| `/mcp` | Official SDK `ModelContextProtocol.AspNetCore` 2.2.0, stateless Streamable HTTP, one tool `whoami` (user id + email from the token). |
| 401 challenge | `WWW-Authenticate: Bearer resource_metadata="{BaseUrl}/.well-known/oauth-protected-resource/mcp"`, emitted by the SDK's `AddMcp()` handler. |
| Protected resource metadata | `GET /.well-known/oauth-protected-resource/mcp` (SDK). `resource = {BaseUrl}/mcp`, `authorization_servers = [{BaseUrl}]`. |
| AS metadata | `GET /.well-known/oauth-authorization-server`: S256 only, `none` auth method, `authorization_response_iss_parameter_supported: true`. |
| DCR | `POST /register`. Stateless: the `client_id` is the HMAC-signed registration (redirect URIs + name), so there's no table and registrations survive restarts and Fly auto-stop. Every redirect URI must be on the allowlist. |
| Authorize | `GET /authorize` checks the client, the redirect URI (registered **and** allowlisted, otherwise an error page, never a redirect), `response_type=code`, PKCE S256 and `resource`. It renders a "Sign in with Google" page that shows the client name and redirect host. `POST /authorize/google` validates the ID token through the existing `AuthService` (same account linking), then redirects with `code`, `state` and `iss`. |
| Token | `POST /token` (form). `authorization_code` checks a single-use code with 60 s expiry, the same client, the same redirect URI, the PKCE verifier and `resource`. `refresh_token` rotates the token (single use). |
| Tokens | Same HS256 key as the REST API, but issuer `{BaseUrl}` and audience `{BaseUrl}/mcp`. A separate `McpBearer` scheme means REST tokens are rejected at `/mcp` and MCP tokens are rejected by the REST API (RFC 8707 audience binding). Access tokens last 60 min. Refresh tokens last 30 days and are stored in the existing `RefreshTokens` table. |
| Redirect allowlist | Config `Mcp:AllowedRedirectUris`. Matching is exact, except loopback `http://localhost/...` and `http://127.0.0.1/...`, which match any port (RFC 8252). |
| Tests | `Babylon.Alfred.Api.Tests/Features/Mcp/` (65 tests): PKCE with the RFC 7636 vector, allowlist rejection cases, code single-use and expiry, `whoami` scoping, a `WebApplicationFactory` test for 401 + header, a REST token rejected at `/mcp`, and a real `tools/call whoami` over HTTP. |

Small changes outside the slice:
- `AuthService.AuthenticateGoogleUserAsync`, extracted from `GoogleLoginAsync` (no behaviour change).
- A `JwtTokenGenerator.GenerateToken` overload that takes issuer, audience and lifetime.
- Forwarded headers (`X-Forwarded-Proto`) in `Program.cs`, so metadata URLs are `https` behind Fly.
- `public partial class Program` for tests.

## Not done / gaps (in priority order)

1. **Refresh tokens share the REST table.** This has three consequences:
   - A REST login (`RevokeAllUserTokensAsync`) also kills MCP sessions, so Claude or ChatGPT has to reconnect.
   - MCP refresh tokens aren't bound to a `client_id`.
   - An MCP refresh token is also accepted by `/api/v1/auth/refresh`.

   The fix is a nullable `ClientId` column on `RefreshToken`: revoke-all would skip MCP tokens, `/token` would check the client, and REST refresh would reject MCP tokens. It needs a migration, which you apply manually. About 2 h.
2. **CIMD is not supported.** The 2026-07-28 spec makes Client ID Metadata Documents the preferred method and marks DCR *deprecated* (still allowed). Claude, Claude Code and ChatGPT all fall back to DCR when the AS doesn't advertise `client_id_metadata_document_supported`, so DCR works today. Adding CIMD means:
   - accepting an HTTPS `client_id` URL, fetching it with SSRF guards, caching it, and checking redirect URIs against it plus the allowlist;
   - advertising the flag.

   About 0.5–1 day. This contradicts the decision "MCP auth = DCR", so worth revisiting in `decisions.md`.
3. **Single instance only.** Pending sign-ins and auth codes are in memory (minutes and seconds). That's fine for one Fly machine. A restart only aborts a sign-in that's in flight.
4. **No scopes.** Nothing in `scopes_supported` and no `scope` in the challenge (both are SHOULD). One "full access" grant. Add scopes only if read-only connectors become useful.
5. **No revocation endpoint and no consent screen beyond the Google page.** Disconnecting in the client simply drops its tokens, and refresh tokens expire after 30 days.
6. **No CORS on `/mcp`.** Browser-based clients such as MCP Inspector-in-browser would need it. claude.ai and ChatGPT connect server-side.
7. **No rate limiting on `/register` or `/token`.** Registration is stateless, so spam costs nothing to store.
8. **Not verified against real Google or real clients.** That needs a deploy (manual steps below).

## Client callback URLs found

| Client | Redirect URI | Registration |
|---|---|---|
| claude.ai, Claude Desktop, mobile | `https://claude.ai/api/mcp/auth_callback` (`https://claude.com/api/mcp/auth_callback` also allowlisted, announced as a possible future URL) | DCR, or CIMD if advertised |
| Claude Code | `http://localhost:<random port>/callback` (fixed port with `--callback-port`). Some versions used `127.0.0.1`. | CIMD client `https://claude.ai/oauth/claude-code-client-metadata`, falls back to DCR |
| ChatGPT | `https://chatgpt.com/connector_platform_oauth_redirect` (the stable URI, used when the AS returns `iss`, which we do). Otherwise `https://chatgpt.com/connector/oauth/{callback_id}`, shown on the connector's management page. | CIMD preferred, DCR supported |

## Manual steps for Brian

1. **Google Cloud console**, existing OAuth client `68098676036-…`. Under *Authorized JavaScript origins*, add:
   - `https://babylon-api.fly.dev` (or whatever the public API host is),
   - `http://localhost` and `http://localhost:5000` for local testing.

   No redirect URIs are needed, because the button uses the popup/JS-callback flow.
2. **Fly config.**
   - Confirm the public host. If it isn't `https://babylon-api.fly.dev`, run `fly secrets set Mcp__BaseUrl=https://<host> -a babylon-api` (not secret, but env overrides config), or change `appsettings.json`.
   - **Check that `Authentication__Jwt__SecretKey` is a real secret on Fly** (`fly secrets list -a babylon-api`). `appsettings.json` only has a placeholder. If prod uses it, anyone can mint tokens for both the REST API and MCP.
3. **Deploy**: `fly deploy -c fly.api.toml`. Smoke test:
   - `curl -si -X POST https://<host>/mcp` should return 401 with the `WWW-Authenticate` header above.
   - `curl https://<host>/.well-known/oauth-authorization-server` should return the AS metadata.
4. **Claude Code**: `claude mcp add --transport http babylon https://<host>/mcp`, then `/mcp` → authenticate → Google sign-in → ask it to call `whoami`.
5. **claude.ai**: Settings → Connectors → Add custom connector → `https://<host>/mcp`. Desktop picks it up from the account.
6. **ChatGPT**: developer mode → add MCP server `https://<host>/mcp`. If the management page shows a `connector/oauth/{callback_id}` redirect, add it to `Mcp:AllowedRedirectUris`.
7. Before connecting real clients, decide whether gap 1 (the `ClientId` migration) ships first. Otherwise every web-app login disconnects the MCP clients.

## Remaining effort

About 1–2 days to "production-ready for me":
- gap 1 (about 2 h),
- deploy plus real-client debugging (half a day; this is where surprises usually show up),
- optional CIMD (0.5–1 day).

MCP tools themselves are step 7.

## Recommendation

**Keep the authorization server in the API.** It's about 650 lines of non-test code in one slice, reuses Google sign-in, users and refresh tokens, adds no new service or vendor account, and passes the protocol checks locally.

Fallback, if real-client testing turns up something nasty: **WorkOS AuthKit**. It is a hosted AS with DCR and CIMD built for MCP, and federates Google. The API would then validate WorkOS-issued JWTs (JWKS) instead of issuing its own, and user mapping would be by email.

## Sources

- MCP authorization 2026-07-28: https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization (plus `/authorization-server-discovery`, `/client-registration`)
- C# SDK: https://github.com/modelcontextprotocol/csharp-sdk (`samples/ProtectedMcpServer`, `src/ModelContextProtocol.AspNetCore/Authentication/McpAuthenticationHandler.cs`), NuGet `ModelContextProtocol.AspNetCore` 2.2.0
- Claude connectors auth: https://claude.com/docs/connectors/building/authentication
- Claude Code MCP: https://code.claude.com/docs/en/mcp
- ChatGPT auth: https://developers.openai.com/plugins/build/auth
- Google Identity Services: https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid
