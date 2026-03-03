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
- Supports account linking (existing local user can link Google account).

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

```
Features/Authentication/
├── Controllers/
│   └── AuthController.cs        # POST: google, login, register, refresh, logout
├── Services/
│   ├── IAuthService.cs          # Interface: GoogleLoginAsync, LoginAsync, RegisterAsync, RefreshTokenAsync, LogoutAsync
│   └── AuthService.cs           # Implementation with UserRepository + RefreshTokenRepository
├── Models/
│   ├── AuthResponse.cs          # { AccessToken, RefreshToken, ExpiresIn }
│   ├── LoginRequest.cs          # { Username, Password }
│   ├── RegisterRequest.cs       # { Username, Email, Password }
│   ├── GoogleLoginRequest.cs    # { IdToken }
│   └── RefreshTokenRequest.cs   # { RefreshToken }
└── Utils/
    └── JwtTokenGenerator.cs     # Generates access tokens (JWT) and refresh tokens (random bytes)
```

## Dependencies

- `IUserRepository` - User CRUD operations
- `IRefreshTokenRepository` - Refresh token CRUD + revocation
- `IConfiguration` - JWT settings, Google ClientId
- `Google.Apis.Auth.GoogleJsonWebSignature` - Google IdToken validation

## Security Notes

- Never return password hashes in responses.
- Refresh tokens are single-use (revoked after refresh).
- All previous refresh tokens for a user are revoked on new login (prevents token reuse).
- `RequireHttpsMetadata = false` in development (should be `true` in production).
