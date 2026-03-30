# Authentication Feature

## Core Principle

**One email = one user account**, regardless of auth method. Users can authenticate via Google OR local password OR both. Accounts are automatically linked by email on first use of the alternate method.

---

## Components

| Component | Responsibility |
|-----------|---------------|
| `AuthController` | Thin HTTP layer, returns `ApiResponse<T>` |
| `AuthService` | Auth flow orchestration (Facade) |
| `AccountLinkingService` | Unified account management, auth method linking (SRP) |
| `JwtTokenGenerator` | JWT + refresh token generation (pure utility) |
| `UserRepository` | User CRUD, email/username lookups |
| `RefreshTokenRepository` | Token CRUD, revocation |

---

## Auth Methods

### Local Auth
- Registration: username + email + password (BCrypt, work factor 11)
- Login: email OR username + password
- `HasLocalAuth = true` when password is set

### Google OAuth
- Client sends Google `IdToken` (obtained via Google Sign-In SDK on frontend)
- Backend validates via `GoogleJsonWebSignature.ValidateAsync()` — **always validate, never trust client email**
- New user: auto-creates account with email as username
- Existing user: links Google to existing account (preserves username)

---

## JWT

- **Algorithm**: HS256
- **Expiry**: 24 hours (1440 minutes), zero clock skew
- **Claims**: Sub (user GUID), Email, UniqueName, AuthProvider
- **Issuer**: `BabylonAlfredApi` / **Audience**: `BabylonAlfredClient`

## Refresh Tokens

- Format: Random 32-byte Base64 string
- Expiry: 7 days (configurable via `Authentication:Jwt:RefreshTokenExpirationDays`)
- **Single-use**: revoked immediately after refresh
- **New login revokes all previous tokens** (prevents session hijacking)
- `IsActive = !IsRevoked && !IsExpired`

---

## Authentication Flows

### Flow 1: Google Login — New User
`GoogleLogin → validate IdToken → no user found → CreateUser { Email, Username=Email, AuthProvider="Google" } → JWT + RefreshToken`

### Flow 2: Google Login — Existing Local User
`GoogleLogin → validate IdToken → user found → LinkGoogleToAccount → AuthProvider="Local,Google" → JWT + RefreshToken`

### Flow 3: Local Registration — New User
`Register → check email (not found) → check username (not found) → BCrypt hash → CreateUser { AuthProvider="Local" } → JWT + RefreshToken`

### Flow 4: Local Registration — Existing Google User
`Register → check email (found, HasLocalAuth=false) → BCrypt hash → LinkLocalToAccount → AuthProvider="Local,Google" → JWT + RefreshToken`

### Flow 5: Local Login
`Login → GetUserByEmailOrUsername → HasLocalAuth=true → BCrypt.Verify → JWT + RefreshToken`

### Flow 6: Token Refresh
`Refresh → GetByToken → IsActive=true → revoke old → GenerateAuthResponse (new JWT + new RefreshToken)`

---

## API Endpoints

| Method | Route | Auth |
|--------|-------|------|
| POST | `/api/v1/auth/register` | Public |
| POST | `/api/v1/auth/login` | Public |
| POST | `/api/v1/auth/google` | Public |
| POST | `/api/v1/auth/refresh` | Public |
| POST | `/api/v1/auth/logout` | Public |
| POST | `/api/v1/me/password` | JWT required |

### Password Update (`POST /api/v1/me/password`)
- Local auth users: `currentPassword` required and verified before updating
- Google-only users: `currentPassword` not required — on success, `AuthProvider` updated to `"Local,Google"`

---

## Security Invariants (Do Not Change Without Security Review)

1. **Passwords**: Always BCrypt hash. Never return `Password` field in API responses.
2. **Refresh tokens**: Single-use. All previous tokens revoked on new login.
3. **Google OAuth**: Always validate `IdToken`. Verify `Audience` matches configured `ClientId`.
4. **Account linking**: Only link by matching email. Preserve original username. Update `AuthProvider` atomically.
5. **Error messages**: Generic "Invalid credentials" for login failures (prevents user enumeration). Specific errors only for registration conflicts.

---

## User Entity (Key Fields)

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }      // Required, unique
    public string? Password { get; set; }     // Nullable (Google-only users)
    public string Email { get; set; }         // Required, unique
    public string? AuthProvider { get; set; } // "Local", "Google", "Local,Google"

    // Computed (NotMapped)
    public bool HasLocalAuth => !string.IsNullOrEmpty(Password);
    public bool HasGoogleAuth => AuthProvider?.Contains("Google") ?? false;
}
```

---

## Test Anchors (Must Have Coverage)

### Registration
- New user (unique username/email) → success
- Duplicate username → `InvalidOperationException`
- Duplicate email with password → `InvalidOperationException`
- Duplicate email (Google-only) → link local auth, success

### Login
- Valid email + password → success
- Valid username + password → success
- Invalid password → `UnauthorizedAccessException`
- Google-only user (no password) → `UnauthorizedAccessException`
- Non-existent user → `UnauthorizedAccessException`

### Google Login
- Valid IdToken (new user) → create account, success
- Valid IdToken (existing local user) → link Google, preserve username, success
- Valid IdToken (existing Google user) → login, success
- Invalid IdToken → `UnauthorizedAccessException`

### Account Linking
- Google login with existing local account → `AuthProvider = "Local,Google"`
- Local registration with existing Google account → `AuthProvider = "Local,Google"`
- Username preserved on Google linking
- `HasLocalAuth` and `HasGoogleAuth` computed correctly

### Refresh Tokens
- Valid active token → new token pair, old token revoked
- Expired token → `UnauthorizedAccessException`
- Revoked token → `UnauthorizedAccessException`
- New login revokes all previous tokens

### Password Update
- Local user + valid currentPassword → success
- Local user + null currentPassword → `UnauthorizedAccessException("Current password is required")`
- Local user + wrong currentPassword → `UnauthorizedAccessException("Invalid current password")`
- Google-only user → no currentPassword required → sets password, `AuthProvider = "Local,Google"`
- Non-existent user → `InvalidOperationException`
- New password always BCrypt hashed (never plaintext)

### Error Handling (GlobalErrorHandlerMiddleware)
- `UnauthorizedAccessException` → HTTP 401
- `InvalidOperationException` → HTTP 400
- Unhandled exceptions → HTTP 500

---

## Configuration

```json
{
  "Authentication": {
    "Jwt": {
      "SecretKey": "...",
      "Issuer": "BabylonAlfredApi",
      "Audience": "BabylonAlfredClient",
      "ExpirationMinutes": "1440",
      "RefreshTokenExpirationDays": "7"
    },
    "Google": {
      "ClientId": "...apps.googleusercontent.com"
    }
  }
}
```
