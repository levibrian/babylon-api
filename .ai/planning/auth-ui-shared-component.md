# Auth UI: shared login component (future)

Note for when `babylon-app` gets its full redesign (React/Expo). Related: [[mcp-auth-spike]].

## Current state

The MCP's `/authorize` page is plain server-rendered HTML in `Babylon.Alfred.Mcp`, showing "Sign in with Google" for any OAuth client (Claude, ChatGPT, future first-party apps). This stays as-is for now — no refactor needed while `babylon-app` is still Angular.

## Future direction (not yet scheduled)

When `babylon-app` is rebuilt (likely React + Expo, web + mobile), move the login UI there instead of keeping it in the MCP service:

- `babylon-app` hosts the actual login/auth component (visual design, Google sign-in button, consent screen).
- The MCP's `/authorize` endpoint stops rendering HTML itself — it redirects to the hosted page in `babylon-app`.
- That page calls back into the MCP's existing auth endpoints (Google ID token exchange, authorize decision) via API, same contract as today's HTML flow.
- `babylon-app`'s own web/mobile login becomes a client of the same `/authorize` flow (first-party OAuth client of the Babylon AS), not a separate login form — so there's exactly one UI implementation, used by web app, mobile app, and third-party MCP clients alike. Same pattern Strava uses across its own surfaces.

## Why not now

Building a reusable component for a frontend stack that's about to be replaced would be thrown-away work. Revisit once the `babylon-app` rewrite is actually starting.
