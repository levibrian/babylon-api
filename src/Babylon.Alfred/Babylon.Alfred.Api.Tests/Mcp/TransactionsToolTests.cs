using System.Security.Claims;
using Babylon.Alfred.Api.Features.Investments.Models.Responses;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Mcp.Tools;
using FluentAssertions;
using Moq;

namespace Babylon.Alfred.Api.Tests.Mcp;

public class TransactionsToolTests
{
    [Fact]
    public async Task GetTransactions_ReturnsServiceResultForCallersUserId()
    {
        var userId = Guid.NewGuid();
        var user = Principal(userId);
        var expected = new List<TransactionDto> { new() };
        var transactionService = new Mock<ITransactionService>();
        transactionService.Setup(x => x.GetAllByUser(userId)).ReturnsAsync(expected);

        var result = await TransactionsTool.GetTransactions(user, transactionService.Object);

        result.Should().BeSameAs(expected);
        transactionService.Verify(x => x.GetAllByUser(userId), Times.Once);
    }

    [Fact]
    public async Task GetTransactions_WithoutAuthenticatedUser_Throws()
    {
        var transactionService = new Mock<ITransactionService>();

        Func<Task> act = () => TransactionsTool.GetTransactions(new ClaimsPrincipal(new ClaimsIdentity()), transactionService.Object);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteTransaction_CallsServiceWithCallerUserId()
    {
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var user = Principal(userId);
        var transactionService = new Mock<ITransactionService>();

        await TransactionsTool.DeleteTransaction(user, transactionService.Object, transactionId);

        transactionService.Verify(x => x.Delete(userId, transactionId), Times.Once);
    }

    [Fact]
    public async Task DeleteTransaction_WithoutAuthenticatedUser_Throws()
    {
        var transactionService = new Mock<ITransactionService>();

        Func<Task> act = () => TransactionsTool.DeleteTransaction(new ClaimsPrincipal(new ClaimsIdentity()), transactionService.Object, Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static ClaimsPrincipal Principal(Guid userId)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
}
