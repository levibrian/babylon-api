# Authentication Feature

## Product Overview

Handles user registration, login, and session management for the Babylon platform. Supports two authentication providers: local (username/password) and Google OAuth. All protected API endpoints require a valid JWT bearer token.

## Business Requirements

### Registration
- Users register with username, email, and password.
- Passwords are hashed with BCrypt before storage.
- `AuthProvider` is set to `"Local"` for local registrations.
- Duplicate username/email validation is enforced.

### Local Login
- Validates username and password against BCrypt hash.
- On success: generates JWT access token + refresh token.
- Previous refresh tokens for the user are revoked on new login.

### Google OAuth Login
- Client sends Google `IdToken` from frontend.
- Backend validates token via `Google.Apis.Auth`.
- If user exists: updates auth provider, generates tokens.
- If user does not exist: auto-creates user account from Google profile.
- **Account linking rule**: If email matches existing local user, link Google provider to that user. Username is NOT changed.

### JWT Tokens
- **Access token**: HS256, 24-hour expiration, zero clock skew.
- Claims: `Sub` (user GUID), `Email`, `UniqueName`, `AuthProvider`.
- Issuer: `BabylonAlfredApi`, Audience: `BabylonAlfredClient`.

### Refresh Tokens
- Random 32-byte token, stored in `refresh_tokens` table.
- 7-day expiration (configurable).
- `IsRevoked` flag for explicit revocation.
- `IsExpired` computed from `ExpiresAt`.
- `IsActive` = not revoked AND not expired.
- On refresh: old token revoked, new access + refresh token pair issued.

### Logout
- Revokes the provided refresh token.

## Architecture

| Component | Purpose |
|-----------|---------|
| **AuthController** | POST: google, login, register, refresh, logout |
| **AuthService** | Business logic: GoogleLoginAsync, LoginAsync, RegisterAsync, RefreshTokenAsync, LogoutAsync |
| **JwtTokenGenerator** | Generates JWT access tokens + random refresh tokens |
| **Models** | AuthResponse, LoginRequest, RegisterRequest, GoogleLoginRequest, RefreshTokenRequest |

## Dependencies

- `IUserRepository` - User CRUD operations
- `IRefreshTokenRepository` - Refresh token CRUD + revocation
- `IConfiguration` - JWT settings, Google ClientId
- `Google.Apis.Auth.GoogleJsonWebSignature` - Google IdToken validation

## Security Invariants

**Critical security rules** (do not modify without security review):

- Never return password hashes in responses
- Refresh tokens are single-use (revoked after refresh)
- All previous refresh tokens revoked on new login (prevents token reuse)
- `RequireHttpsMetadata = false` in development only (MUST be `true` in production)
- Password validation: BCrypt work factor configured for adequate complexity

## Test Anchors

Test scenarios that MUST have coverage:

- Duplicate registration (username or email) → throw
- Invalid Google token → throw
- Expired refresh token → reject
- Revoked token reuse → reject
- Account linking: email match links to existing user, username preserved
