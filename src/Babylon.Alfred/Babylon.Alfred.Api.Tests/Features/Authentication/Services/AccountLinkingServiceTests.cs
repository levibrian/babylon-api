using AutoFixture;
using Babylon.Alfred.Api.Features.Authentication.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Features.Authentication.Services;

public class AccountLinkingServiceTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly AccountLinkingService sut;

    public AccountLinkingServiceTests()
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        sut = autoMocker.CreateInstance<AccountLinkingService>();
    }

    [Fact]
    public async Task GetOrCreateGoogleUserAsync_NewUser_ShouldCreateUser()
    {
        // Arrange
        var email = "newuser@example.com";
        var googleSubject = "google-sub-123";

        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.GetUserByEmailAsync(email))
            .ReturnsAsync((User?)null);

        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await sut.GetOrCreateGoogleUserAsync(email, googleSubject);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(email);
        result.Username.Should().Be(email); // Default to email
        result.AuthProvider.Should().Be("Google");
        result.Password.Should().BeNull();

        autoMocker.GetMock<IUserRepository>()
            .Verify(x => x.CreateUserAsync(It.Is<User>(u =>
                u.Email == email &&
                u.Username == email &&
                u.AuthProvider == "Google")), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateGoogleUserAsync_ExistingUser_ShouldLinkGoogleAuth()
    {
        // Arrange
        var email = "existing@example.com";
        var googleSubject = "google-sub-456";

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = "originalusername",
            Password = "hashed-password",
            AuthProvider = "Local"
        };

        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.GetUserByEmailAsync(email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await sut.GetOrCreateGoogleUserAsync(email, googleSubject);

        // Assert
        result.Should().BeSameAs(existingUser);
        result.Username.Should().Be("originalusername"); // Preserved
        result.AuthProvider.Should().Be("Local,Google");

        autoMocker.GetMock<IUserRepository>()
            .Verify(x => x.UpdateUserAsync(existingUser), Times.Once);
    }

    [Fact]
    public async Task LinkGoogleToAccountAsync_NewLink_ShouldUpdateAuthProvider()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "username",
            Password = "hashed-password",
            AuthProvider = "Local"
        };

        // Act
        await sut.LinkGoogleToAccountAsync(user);

        // Assert
        user.AuthProvider.Should().Be("Local,Google");
        autoMocker.GetMock<IUserRepository>()
            .Verify(x => x.UpdateUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task LinkGoogleToAccountAsync_AlreadyLinked_ShouldNotUpdate()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "username",
            Password = "hashed-password",
            AuthProvider = "Local,Google"
        };

        // Act
        await sut.LinkGoogleToAccountAsync(user);

        // Assert
        user.AuthProvider.Should().Be("Local,Google");
        autoMocker.GetMock<IUserRepository>()
            .Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LinkLocalToAccountAsync_GoogleOnlyAccount_ShouldAddPassword()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "username",
            Password = null,
            AuthProvider = "Google"
        };

        var passwordHash = "new-hashed-password";

        // Act
        await sut.LinkLocalToAccountAsync(user, passwordHash);

        // Assert
        user.Password.Should().Be(passwordHash);
        user.AuthProvider.Should().Be("Local,Google");
        autoMocker.GetMock<IUserRepository>()
            .Verify(x => x.UpdateUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task LinkLocalToAccountAsync_AlreadyHasPassword_ShouldThrow()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "username",
            Password = "existing-password",
            AuthProvider = "Local"
        };

        var passwordHash = "new-hashed-password";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.LinkLocalToAccountAsync(user, passwordHash));

        autoMocker.GetMock<IUserRepository>()
            .Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public void UpdateAuthProvider_LocalOnly_ShouldSetLocal()
    {
        // Arrange
        var user = new User
        {
            Password = "hashed-password",
            AuthProvider = null
        };

        // Act
        sut.UpdateAuthProvider(user);

        // Assert
        user.AuthProvider.Should().Be("Local");
    }

    [Fact]
    public void UpdateAuthProvider_GoogleOnly_ShouldSetGoogle()
    {
        // Arrange
        var user = new User
        {
            Password = null,
            AuthProvider = "Google"
        };

        // Act
        sut.UpdateAuthProvider(user);

        // Assert
        user.AuthProvider.Should().Be("Google");
    }

    [Fact]
    public void UpdateAuthProvider_BothMethods_ShouldSetBoth()
    {
        // Arrange
        var user = new User
        {
            Password = "hashed-password",
            AuthProvider = "Google"
        };

        // Act
        sut.UpdateAuthProvider(user);

        // Assert
        user.AuthProvider.Should().Be("Local,Google");
    }

    [Fact]
    public void UpdateAuthProvider_NoMethods_ShouldSetNull()
    {
        // Arrange
        var user = new User
        {
            Password = null,
            AuthProvider = null
        };

        // Act
        sut.UpdateAuthProvider(user);

        // Assert
        user.AuthProvider.Should().BeNull();
    }
}
