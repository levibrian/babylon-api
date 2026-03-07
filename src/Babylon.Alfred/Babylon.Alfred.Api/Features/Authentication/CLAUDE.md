# Authentication Feature

## Product Overview

Handles user registration, login, and session management for the Babylon platform. Supports **unified authentication** with two methods: local (email/username + password) and Google OAuth. Users can authenticate with either method, and accounts are **automatically linked by email**. All protected API endpoints require a valid JWT bearer token.

## Core Principles

### ✅ Unified Account Management
- **One email = One user account** (regardless of auth method)
- Users can authenticate via Google OR local password OR both
- Account linking happens automatically on first use of alternate auth method
- `AuthProvider` field tracks enabled methods: `"Local"`, `"Google"`, or `"Local,Google"`

### ✅ Backwards Compatibility
- Existing users with `"Local"` can add Google auth seamlessly
- Existing users with `"Google"` can add password auth seamlessly
- Username preserved on account linking (never overwritten)

## Business Requirements

### Registration (Local)
- Users register with username, email, and password
- Passwords hashed with BCrypt (work factor 11)
- `AuthProvider` set to `"Local"`
- **If email exists (Google-only account)**: Links password to existing account instead of throwing error
- **If username taken**: Throws `InvalidOperationException`
- **If email exists (with password)**: Throws `InvalidOperationException`

### Local Login
- Accepts **email OR username** + password
- Validates password against BCrypt hash
- Only works if user has `HasLocalAuth == true` (password set)
- On success: generates JWT access token + refresh token
- Previous refresh tokens revoked on new login

### Google OAuth Login
- Client sends Google `IdToken` from frontend (obtained via Google Sign-In SDK)
- Backend validates token via `Google.Apis.Auth.GoogleJsonWebSignature`
- **If user does NOT exist**: Auto-creates account with email as username
- **If user exists by email**: Links Google auth to existing account (preserves username)
- On success: generates JWT access token + refresh token

### JWT Tokens
- **Algorithm**: HS256
- **Expiration**: 24 hours (1440 minutes)
- **Clock skew**: Zero tolerance
- **Claims**:
  - `Sub`: User GUID
  - `Email`: User email
  - `UniqueName`: Username
  - `AuthProvider`: Current provider state (`"Local"`, `"Google"`, or `"Local,Google"`)
- **Issuer**: `BabylonAlfredApi`
- **Audience**: `BabylonAlfredClient`

### Refresh Tokens
- **Format**: Random 32-byte Base64 string
- **Storage**: `refresh_tokens` table with FK to User
- **Expiration**: 7 days (configurable via `Authentication:Jwt:RefreshTokenExpirationDays`)
- **Single-use**: Revoked immediately after refresh
- **Active if**: `!IsRevoked AND ExpiresAt > UtcNow`
- **Security**: All previous refresh tokens revoked on new login

### Logout
- Revokes the provided refresh token
- Client must discard access token (stateless, cannot be revoked server-side)

## Architecture

### Component Diagram

\`\`\`
┌─────────────────────┐
│   AuthController    │  (Thin, returns ApiResponse<T>)
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│    IAuthService     │  (Orchestrates auth flows)
│   (AuthService)     │
└──────────┬──────────┘
           │
           ├──────────────┐
           ▼              ▼
┌──────────────────┐  ┌────────────────────────┐
│ IAccountLinking  │  │  JwtTokenGenerator     │
│     Service      │  │  (Token generation)    │
│ (Account linking)│  └────────────────────────┘
└──────────┬───────┘
           │
           ▼
┌─────────────────────┐
│  IUserRepository    │  (Data access)
│IRefreshTokenRepo    │
└─────────────────────┘
\`\`\`

### Service Responsibilities (SOLID)

| Service | Responsibility | Pattern |
|---------|---------------|---------|
| **AuthController** | HTTP request/response handling, returns `ApiResponse<T>` | Controller |
| **AuthService** | Auth flow orchestration, token generation coordination | Facade |
| **AccountLinkingService** | Unified account management, auth method linking logic | SRP |
| **JwtTokenGenerator** | JWT + refresh token generation (pure utility) | Utility |
| **UserRepository** | User CRUD, email/username lookups | Repository |
| **RefreshTokenRepository** | Token CRUD, revocation | Repository |

### Key Design Patterns

- **Facade Pattern**: `AuthService` orchestrates multiple services
- **Single Responsibility**: `AccountLinkingService` handles ONLY account linking logic
- **Dependency Inversion**: Services depend on interfaces (`IUserRepository`, `IAccountLinkingService`)
- **DRY**: `GenerateAuthResponseAsync()` eliminates token generation duplication

## Authentication Flows

### Flow 1: Google Login (New User)

\`\`\`
User → Frontend → AuthController.GoogleLogin()
                       ↓
                  AuthService.GoogleLoginAsync()
                       ↓
              [Validate Google IdToken]
                       ↓
          AccountLinkingService.GetOrCreateGoogleUserAsync()
                       ↓
              [No user found by email]
                       ↓
           UserRepository.CreateUserAsync()
           { Email, Username=Email, AuthProvider="Google" }
                       ↓
          AuthService.GenerateAuthResponseAsync()
          [Generate JWT + RefreshToken, Revoke old tokens]
                       ↓
              ApiResponse<AuthResponse>
\`\`\`

### Flow 2: Google Login (Existing Local User)

\`\`\`
User → Frontend → AuthController.GoogleLogin()
                       ↓
                  AuthService.GoogleLoginAsync()
                       ↓
              [Validate Google IdToken]
                       ↓
          AccountLinkingService.GetOrCreateGoogleUserAsync()
                       ↓
              [User found by email]
                       ↓
        AccountLinkingService.LinkGoogleToAccountAsync()
        [Set AuthProvider = "Local,Google"]
                       ↓
          UserRepository.UpdateUserAsync()
                       ↓
          AuthService.GenerateAuthResponseAsync()
                       ↓
              ApiResponse<AuthResponse>
\`\`\`

### Flow 3: Local Registration (New User)

\`\`\`
User → Frontend → AuthController.Register()
                       ↓
                  AuthService.RegisterAsync()
                       ↓
           [Check email: UserRepository.GetUserByEmailAsync()]
                       ↓
                  [Not found]
                       ↓
           [Check username: UserRepository.GetUserByUsernameAsync()]
                       ↓
                  [Not found]
                       ↓
              [Hash password with BCrypt]
                       ↓
           UserRepository.CreateUserAsync()
           { Username, Email, Password, AuthProvider="Local" }
                       ↓
          AuthService.GenerateAuthResponseAsync()
                       ↓
              ApiResponse<AuthResponse>
\`\`\`

### Flow 4: Local Registration (Existing Google User)

\`\`\`
User → Frontend → AuthController.Register()
                       ↓
                  AuthService.RegisterAsync()
                       ↓
           [Check email: UserRepository.GetUserByEmailAsync()]
                       ↓
              [User found with Google-only]
                       ↓
        [user.HasLocalAuth == false]
                       ↓
              [Hash password with BCrypt]
                       ↓
        AccountLinkingService.LinkLocalToAccountAsync()
        [Set Password, Update AuthProvider="Local,Google"]
                       ↓
          UserRepository.UpdateUserAsync()
                       ↓
          AuthService.GenerateAuthResponseAsync()
                       ↓
              ApiResponse<AuthResponse>
\`\`\`

### Flow 5: Local Login (Email or Username)

\`\`\`
User → Frontend → AuthController.Login()
                       ↓
                  AuthService.LoginAsync()
                       ↓
       UserRepository.GetUserByEmailOrUsernameAsync(emailOrUsername)
                       ↓
          [User found AND user.HasLocalAuth == true]
                       ↓
              [BCrypt.Verify(password, user.Password)]
                       ↓
                    [Valid]
                       ↓
          AuthService.GenerateAuthResponseAsync()
                       ↓
              ApiResponse<AuthResponse>
\`\`\`

### Flow 6: Token Refresh

\`\`\`
User → Frontend → AuthController.Refresh()
                       ↓
                  AuthService.RefreshTokenAsync()
                       ↓
          RefreshTokenRepository.GetByTokenAsync()
                       ↓
        [storedToken.IsActive == true]
                       ↓
        [Set storedToken.IsRevoked = true]
        RefreshTokenRepository.UpdateAsync()
                       ↓
          AuthService.GenerateAuthResponseAsync()
          [Generate new JWT + new RefreshToken]
                       ↓
              ApiResponse<AuthResponse>
\`\`\`

## Data Model

### User Entity (Enhanced)

\`\`\`csharp
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }         // Required, unique
    public string? Password { get; set; }        // Nullable (Google-only users)
    public string Email { get; set; }            // Required, unique
    public string? AuthProvider { get; set; }    // "Local", "Google", "Local,Google"

    // Computed properties
    public bool HasLocalAuth => !string.IsNullOrEmpty(Password);
    public bool HasGoogleAuth => AuthProvider?.Contains("Google") ?? false;

    // Navigation
    public ICollection<RefreshToken> RefreshTokens { get; set; }
}
\`\`\`

### RefreshToken Entity

\`\`\`csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; }            // 32-byte Base64
    public Guid UserId { get; set; }
    public User User { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }

    // Computed
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
\`\`\`

## API Endpoints

### POST `/api/v1/auth/register`

**Request**:
\`\`\`json
{
  "username": "johndoe",
  "email": "john@example.com",
  "password": "SecureP@ss123"
}
\`\`\`

**Response** (`200 OK`):
\`\`\`json
{
  "success": true,
  "data": {
    "token": "eyJhbGc...",
    "refreshToken": "abc123...",
    "userId": "guid",
    "username": "johndoe",
    "email": "john@example.com",
    "authProvider": "Local"
  }
}
\`\`\`

**Errors**:
- `400`: Username taken or email already registered with password

### POST `/api/v1/auth/login`

**Request**:
\`\`\`json
{
  "emailOrUsername": "john@example.com",  // Can be email OR username
  "password": "SecureP@ss123"
}
\`\`\`

**Response** (`200 OK`): Same as register

**Errors**:
- `401`: Invalid credentials (wrong password, user not found, or no password set)

### POST `/api/v1/auth/google`

**Request**:
\`\`\`json
{
  "idToken": "google-id-token-from-frontend"
}
\`\`\`

**Response** (`200 OK`): Same as register, `authProvider` may be `"Google"` or `"Local,Google"`

**Errors**:
- `401`: Invalid or expired Google token

### POST `/api/v1/auth/refresh`

**Request**:
\`\`\`json
{
  "refreshToken": "abc123..."
}
\`\`\`

**Response** (`200 OK`): New token pair (old refresh token revoked)

**Errors**:
- `401`: Invalid, expired, or revoked refresh token

### POST `/api/v1/auth/logout`

**Request**:
\`\`\`json
{
  "refreshToken": "abc123..."
}
\`\`\`

**Response** (`200 OK`):
\`\`\`json
{
  "success": true,
  "data": {}
}
\`\`\`

### POST `/api/v1/me/password` (Requires JWT)

Updates or sets the authenticated user's password.

- **Local auth users**: `currentPassword` is required and verified before updating.
- **Google-only users**: `currentPassword` is not required (no existing password). On success, `AuthProvider` is updated from `"Google"` to `"Local,Google"`, enabling both login methods.

**Request**:
\`\`\`json
{
  "currentPassword": "OldPass1!",
  "password": "NewPass1!"
}
\`\`\`

**Response** (`200 OK`):
\`\`\`json
{
  "success": true,
  "data": {}
}
\`\`\`

**Errors**:
- `400`: User not found
- `401`: `currentPassword` missing or wrong (local auth users only)

## Security Invariants

**Critical security rules** (do not modify without security review):

1. **Password Handling**:
   - Always hash with BCrypt (never store plaintext)
   - Never return `Password` field in API responses
   - Password required for `HasLocalAuth == true`

2. **Token Security**:
   - Refresh tokens are **single-use** (revoked immediately after refresh)
   - All previous refresh tokens revoked on new login (prevents session hijacking)
   - Access tokens are stateless (cannot be revoked, rely on short expiration)
   - Zero clock skew tolerance

3. **Google OAuth**:
   - Always validate `IdToken` via `GoogleJsonWebSignature.ValidateAsync()`
   - Verify `Audience` matches configured `Authentication:Google:ClientId`
   - Never trust client-provided email without validation

4. **Account Linking**:
   - Only link accounts with matching email addresses
   - Preserve original username on account linking
   - Update `AuthProvider` atomically with password/linking changes

5. **Error Messages**:
   - Generic "Invalid credentials" for login failures (prevents user enumeration)
   - Specific errors only for registration conflicts (username/email taken)

## Configuration

\`\`\`json
{
  "Authentication": {
    "Jwt": {
      "SecretKey": "your-256-bit-secret",
      "Issuer": "BabylonAlfredApi",
      "Audience": "BabylonAlfredClient",
      "ExpirationMinutes": "1440",  // 24 hours
      "RefreshTokenExpirationDays": "7"
    },
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com"
    }
  }
}
\`\`\`

## TDD Workflow for Authentication Changes

### 🚨 BEFORE MODIFYING ANY AUTHENTICATION CODE 🚨

**STOP and follow this workflow:**

1. **Read Test Anchors** (below) - Understand all required test scenarios
2. **Write Tests First** - Create failing tests for your change
3. **Run Tests** - Verify they fail (Red)
4. **Implement** - Write minimum code to pass
5. **Run Tests** - Verify they pass (Green)
6. **Refactor** - Keep tests green
7. **Update CLAUDE.md** - Document new invariants

**Never write authentication code without tests first. Authentication bugs = security vulnerabilities.**

### Test File Location
- `Babylon.Alfred.Api.Tests/Features/Authentication/Services/AuthServiceTests.cs`
- `Babylon.Alfred.Api.Tests/Features/Authentication/Services/AccountLinkingServiceTests.cs`
- `Babylon.Alfred.Api.Tests/Features/Authentication/Controllers/AuthControllerTests.cs`

### Required Test Mocks
- `Mock<IUserRepository>`
- `Mock<IRefreshTokenRepository>`
- `Mock<IAccountLinkingService>` (for AuthService tests)
- `Mock<JwtTokenGenerator>` (use `autoMocker.GetMock<JwtTokenGenerator>()`)
- `Mock<IConfiguration>` (setup JWT and Google settings)
- `Mock<ILogger<T>>`

### Example Test Structure

```csharp
[Fact]
public async Task GoogleLoginAsync_ExistingLocalUser_LinksGoogleAuthAndPreservesUsername()
{
    // Arrange
    var existingUser = new User
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        Username = "originalusername",
        Password = "hashed-password",
        AuthProvider = "Local"
    };

    _userRepositoryMock
        .Setup(x => x.GetUserByEmailAsync("test@example.com"))
        .ReturnsAsync(existingUser);

    _accountLinkingServiceMock
        .Setup(x => x.GetOrCreateGoogleUserAsync("test@example.com", It.IsAny<string>()))
        .ReturnsAsync(existingUser);

    // Act
    var result = await _authService.GoogleLoginAsync("valid-google-token");

    // Assert
    result.Should().NotBeNull();
    result.Username.Should().Be("originalusername"); // Username preserved
    result.AuthProvider.Should().Be("Local,Google"); // Both methods enabled

    _accountLinkingServiceMock.Verify(
        x => x.GetOrCreateGoogleUserAsync("test@example.com", It.IsAny<string>()),
        Times.Once);
}
```

## Test Anchors

Test scenarios that MUST have coverage:

### Registration
- ✅ New user with unique username/email → success
- ✅ Duplicate username → throw `InvalidOperationException`
- ✅ Duplicate email (with password) → throw `InvalidOperationException`
- ✅ Duplicate email (Google-only) → link local auth, success

### Login
- ✅ Valid email + password → success
- ✅ Valid username + password → success
- ✅ Invalid password → throw `UnauthorizedAccessException`
- ✅ User without password (Google-only) → throw `UnauthorizedAccessException`
- ✅ Non-existent user → throw `UnauthorizedAccessException`

### Google Login
- ✅ Valid IdToken (new user) → create account, success
- ✅ Valid IdToken (existing local user) → link Google, preserve username, success
- ✅ Valid IdToken (existing Google user) → login, success
- ✅ Invalid IdToken → throw `UnauthorizedAccessException`

### Account Linking
- ✅ Google login with existing local account → `AuthProvider = "Local,Google"`
- ✅ Local registration with existing Google account → `AuthProvider = "Local,Google"`
- ✅ Username preserved on Google linking
- ✅ `HasLocalAuth` and `HasGoogleAuth` computed properties accurate

### Refresh Tokens
- ✅ Valid active token → new token pair, old token revoked
- ✅ Expired token → throw `UnauthorizedAccessException`
- ✅ Revoked token → throw `UnauthorizedAccessException`
- ✅ New login revokes all previous tokens

### Logout
- ✅ Valid token → revoked successfully
- ✅ Invalid token → no error (idempotent)

### Password Update (`POST /api/v1/me/password`)
- ✅ Local user + valid currentPassword → hash and persist new password
- ✅ Local user + null currentPassword → throw `UnauthorizedAccessException("Current password is required")`
- ✅ Local user + wrong currentPassword → throw `UnauthorizedAccessException("Invalid current password")`
- ✅ Google-only user + null currentPassword → set password, update `AuthProvider` to `"Local,Google"`, success
- ✅ Google-only user → no currentPassword required
- ✅ Non-existent user → throw `InvalidOperationException`
- ✅ New password never stored as plaintext (always BCrypt hashed)

### Error Handling (GlobalErrorHandlerMiddleware)
- ✅ `UnauthorizedAccessException` → HTTP 401
- ✅ `InvalidOperationException` → HTTP 400
- ✅ Unhandled exceptions → HTTP 500

## Dependencies

- `IUserRepository` - User CRUD, email/username lookups
- `IRefreshTokenRepository` - Token CRUD, revocation
- `IAccountLinkingService` - Account linking and auth provider management
- `JwtTokenGenerator` - JWT + refresh token generation
- `IConfiguration` - JWT settings, Google ClientId
- `Google.Apis.Auth` - Google IdToken validation
- `BCrypt.Net` - Password hashing/verification

## Files Structure

\`\`\`
Features/Authentication/
├── CLAUDE.md                              (this file)
├── Controllers/
│   └── AuthController.cs                  (Thin, returns ApiResponse<T>)
├── Services/
│   ├── IAuthService.cs                    (Auth orchestration interface)
│   ├── AuthService.cs                     (Auth orchestration implementation)
│   ├── IAccountLinkingService.cs          (Account linking interface)
│   └── AccountLinkingService.cs           (Account linking implementation)
├── Utils/
│   └── JwtTokenGenerator.cs               (Token generation utility)
└── Models/
    ├── AuthResponse.cs                    (Login/register response)
    ├── LoginRequest.cs                    (EmailOrUsername + Password)
    ├── RegisterRequest.cs                 (Username + Email + Password)
    ├── GoogleLoginRequest.cs              (IdToken)
    └── RefreshTokenRequest.cs             (RefreshToken)
\`\`\`

## Migration Notes

### Breaking Changes from Previous Implementation

1. **LoginRequest.Username → EmailOrUsername**: Frontend must update field name
2. **AuthController responses**: Now wrapped in `ApiResponse<T>` (adds `success` and `data` fields)
3. **AuthProvider values**: May now be `"Local,Google"` for hybrid accounts (was single value)

### Database Migration Required

**No schema changes required** - `AuthProvider` column already supports comma-separated values.
