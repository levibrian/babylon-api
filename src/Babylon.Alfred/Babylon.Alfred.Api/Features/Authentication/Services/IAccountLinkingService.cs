using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Features.Authentication.Services;

/// <summary>
/// Handles unified account management and linking between auth providers.
/// Ensures one user per email regardless of auth method.
/// </summary>
public interface IAccountLinkingService
{
    /// <summary>
    /// Gets or creates a user for Google authentication.
    /// Links to existing account if email matches.
    /// </summary>
    Task<User> GetOrCreateGoogleUserAsync(string email, string googleSubject);

    /// <summary>
    /// Links Google authentication to an existing local account.
    /// </summary>
    Task LinkGoogleToAccountAsync(User user);

    /// <summary>
    /// Links local authentication (password) to an existing Google account.
    /// </summary>
    Task LinkLocalToAccountAsync(User user, string passwordHash);

    /// <summary>
    /// Updates AuthProvider field to reflect all enabled auth methods.
    /// </summary>
    void UpdateAuthProvider(User user);
}
