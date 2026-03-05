
# Authentication Feature

## Overview

User registration, login, and session management. Supports local (username/password) and Google OAuth providers. All protected endpoints require a valid JWT bearer token.

## Security Invariants (Non-Negotiable)

- Password hashes must NEVER be returned in responses.
- Refresh tokens are **single-use**: revoked immediately upon use.
- All previous refresh tokens for a user are revoked on new login.
- Account linking: if a Google email matches an existing local user, link the Google provider — do NOT create a duplicate user, do NOT change the username.
- `RequireHttpsMetadata = false` in development only; production must use `true`.

## Business Rules

### Registration
- Fields: `Username`, `Email`, `Password`.
- Password hashed with BCrypt before storage.
- `AuthProvider = "Local"`.
- Duplicate username or email → reject.

### Local Login
- Validate username + BCrypt hash.
- On success: issue JWT access token + refresh token.
- All existing refresh tokens for user → revoked.

### Google OAuth
- Client sends Google `IdToken`.
- Backend validates via `Google.Apis.Auth.GoogleJsonWebSignature`.
- User exists → update auth provider, issue tokens.
- User does not exist → auto-create from Google profile, issue tokens.

### JWT Access Token
- Algorithm: HS256. Expiry: 24h. Clock skew: zero.
- Claims: `Sub` (user GUID), `Email`, `UniqueName`, `AuthProvider`.
- Issuer: `BabylonAlfredApi`. Audience: `BabylonAlfredClient`.

### Refresh Tokens
- 32-byte random token, stored in `refresh_tokens` table.
- Expiry: 7 days (configurable).
- `IsActive = !IsRevoked && !IsExpired`.
- On refresh: old token revoked, new access + refresh pair issued.

### Logout
- Revokes the provided refresh token.

## Component Inventory

| Component | Responsibility |
|-----------|---------------|
| `AuthController` | POST: google, login, register, refresh, logout |
| `AuthService` | All auth business logic |
| `JwtTokenGenerator` | JWT access token + refresh token generation |

Dependencies: `IUserRepository`, `IRefreshTokenRepository`, `IConfiguration`, `Google.Apis.Auth`

## Test Anchors

Tests in `Babylon.Alfred.Api.Tests/Features/Authentication/` must cover:

- Duplicate username → reject
- Duplicate email → reject
- Invalid Google IdToken → reject
- Expired refresh token → reject
- Revoked refresh token → reject (must not allow reuse)
- Refresh token → old token revoked, new tokens issued
- Google login with matching email to existing local user → link (not duplicate)
- Successful local login → all prior refresh tokens revoked
