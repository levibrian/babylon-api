using Babylon.Alfred.Api.Features.Authentication.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Babylon.Alfred.Api.Tests.Features.Authentication.Services;

public class PasswordServiceTests
{
    private readonly AutoMocker autoMocker = new();
    private readonly PasswordService sut;

    public PasswordServiceTests()
    {
        sut = autoMocker.CreateInstance<PasswordService>();
    }

    [Fact]
    public async Task UpdatePassword_LocalUserWithValidCurrentPassword_ShouldHashAndPersistNewPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPassword = "CurrentPass1!";
        var newPassword = "NewPassword1!";
        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            Username = "testuser",
            Password = BCrypt.Net.BCrypt.HashPassword(currentPassword),
            AuthProvider = "Local"
        };

        User? capturedUser = null;
        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.GetUserAsync(userId))
            .ReturnsAsync(user);
        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u);

        // Act
        await sut.UpdatePassword(userId, currentPassword, newPassword);

        // Assert
        autoMocker.GetMock<IUserRepository>().Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Once);
        BCrypt.Net.BCrypt.Verify(newPassword, capturedUser!.Password!).Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePassword_LocalUserWithValidCurrentPassword_ShouldNotStorePlainTextPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPassword = "CurrentPass1!";
        var newPassword = "NewPassword1!";
        var user = new User
        {
            Id = userId,
            Password = BCrypt.Net.BCrypt.HashPassword(currentPassword),
            AuthProvider = "Local"
        };

        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.GetUserAsync(userId))
            .ReturnsAsync(user);

        // Act
        await sut.UpdatePassword(userId, currentPassword, newPassword);

        // Assert
        autoMocker.GetMock<IUserRepository>()
            .Verify(r => r.UpdateUserAsync(It.Is<User>(u => u.Password != newPassword)), Times.Once);
    }

    [Fact]
    public async Task UpdatePassword_LocalUserWithInvalidCurrentPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Password = BCrypt.Net.BCrypt.HashPassword("CorrectPass1!"),
            AuthProvider = "Local"
        };

        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.GetUserAsync(userId))
            .ReturnsAsync(user);

        // Act
        Func<Task> act = () => sut.UpdatePassword(userId, "WrongPass1!", "NewPassword1!");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid current password");
    }

    [Fact]
    public async Task UpdatePassword_LocalUserWithNullCurrentPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Password = BCrypt.Net.BCrypt.HashPassword("CurrentPass1!"),
            AuthProvider = "Local"
        };

        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.GetUserAsync(userId))
            .ReturnsAsync(user);

        // Act
        Func<Task> act = () => sut.UpdatePassword(userId, null, "NewPassword1!");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Current password is required");
    }

    [Fact]
    public async Task UpdatePassword_GoogleOnlyUser_ShouldCallUpdateAuthProviderWithHashedPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newPassword = "NewPassword1!";
        var user = new User
        {
            Id = userId,
            Password = null,
            AuthProvider = "Google"
        };

        User? capturedUser = null;
        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.GetUserAsync(userId))
            .ReturnsAsync(user);
        autoMocker.GetMock<IAccountLinkingService>()
            .Setup(x => x.UpdateAuthProvider(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u);

        // Act
        await sut.UpdatePassword(userId, null, newPassword);

        // Assert
        autoMocker.GetMock<IAccountLinkingService>()
            .Verify(x => x.UpdateAuthProvider(It.Is<User>(u => u.Id == userId)), Times.Once);
        BCrypt.Net.BCrypt.Verify(newPassword, capturedUser!.Password!).Should().BeTrue();

        autoMocker.GetMock<IUserRepository>()
            .Verify(r => r.UpdateUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdatePassword_GoogleOnlyUser_ShouldNotRequireCurrentPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Password = null,
            AuthProvider = "Google"
        };

        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.GetUserAsync(userId))
            .ReturnsAsync(user);

        // Act
        Func<Task> act = () => sut.UpdatePassword(userId, null, "NewPassword1!");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdatePassword_WithNonExistentUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.GetUserAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = () => sut.UpdatePassword(userId, "current", "NewPassword1!");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdatePassword_LocalUserWithValidCurrentPassword_ShouldCallUpdateUserAsyncOnce()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPassword = "CurrentPass1!";
        var user = new User
        {
            Id = userId,
            Password = BCrypt.Net.BCrypt.HashPassword(currentPassword),
            AuthProvider = "Local"
        };

        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.GetUserAsync(userId))
            .ReturnsAsync(user);

        // Act
        await sut.UpdatePassword(userId, currentPassword, "NewPassword1!");

        // Assert
        autoMocker.GetMock<IUserRepository>()
            .Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePassword_LocalUserWithValidCurrentPassword_ShouldCallUpdateAuthProvider()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPassword = "CurrentPass1!";
        var user = new User
        {
            Id = userId,
            Password = BCrypt.Net.BCrypt.HashPassword(currentPassword),
            AuthProvider = "Local"
        };

        autoMocker.GetMock<IUserRepository>()
            .Setup(x => x.GetUserAsync(userId))
            .ReturnsAsync(user);

        // Act
        await sut.UpdatePassword(userId, currentPassword, "NewPassword1!");

        // Assert
        autoMocker.GetMock<IAccountLinkingService>()
            .Verify(x => x.UpdateAuthProvider(user), Times.Once);
    }
}
