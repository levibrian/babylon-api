using AutoFixture;
using Babylon.Alfred.Api.Features.Investments.Controllers;
using Babylon.Alfred.Api.Features.Investments.Models.Requests;
using Babylon.Alfred.Api.Features.Investments.Services;
using Babylon.Alfred.Api.Shared.Data.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;

namespace Babylon.Alfred.Api.Tests.Features.Investments.Controllers;

public class TransactionsControllerTests
{
    private readonly Fixture fixture = new();
    private readonly AutoMocker autoMocker = new();
    private readonly TransactionsController sut;

    public TransactionsControllerTests()
    {
        // Assuming the loop is caused by the 'Transactions' property on the 'Account' class:
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b)); // Remove default behavior
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Configure AutoFixture to handle DateOnly - prevents invalid date generation
        fixture.Customize<DateOnly>(composer => composer.FromFactory(() =>
        {
            var random = new Random();
            var year = random.Next(2020, 2030);
            var month = random.Next(1, 13);
            var day = random.Next(1, DateTime.DaysInMonth(year, month) + 1);
            return new DateOnly(year, month, day);
        }));

        sut = autoMocker.CreateInstance<TransactionsController>();
    }

    [Fact]
    public async Task CreateTransaction_WithValidRequest_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var request = fixture.Create<CreateTransactionRequest>();
        var transaction = fixture.Create<Transaction>();
        autoMocker.GetMock<ITransactionService>().Setup(x => x.Create(request)).ReturnsAsync(transaction);

        // Act
        var result = await sut.CreateTransaction(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = "Successfully stored the transaction" });
        autoMocker.GetMock<ITransactionService>().Verify(x => x.Create(request), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionsBulk_WithValidRequests_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var requests = fixture.CreateMany<CreateTransactionRequest>(5).ToList();
        var transactions = fixture.CreateMany<Transaction>(5).ToList();
        autoMocker.GetMock<ITransactionService>().Setup(x => x.CreateBulk(requests)).ReturnsAsync(transactions);

        // Act
        var result = await sut.CreateTransactionsBulk(requests);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = $"Successfully stored {transactions.Count} transactions" });
        autoMocker.GetMock<ITransactionService>().Verify(x => x.CreateBulk(requests), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionsBulk_WithEmptyList_ShouldReturnOkWithZeroCount()
    {
        // Arrange
        var requests = new List<CreateTransactionRequest>();
        var transactions = new List<Transaction>();
        autoMocker.GetMock<ITransactionService>().Setup(x => x.CreateBulk(requests)).ReturnsAsync(transactions);

        // Act
        var result = await sut.CreateTransactionsBulk(requests);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = "Successfully stored 0 transactions" });
        autoMocker.GetMock<ITransactionService>().Verify(x => x.CreateBulk(requests), Times.Once);
    }

    [Fact]
    public async Task GetTransactions_WithUserId_ShouldReturnOkWithTransactions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactions = fixture.CreateMany<Babylon.Alfred.Api.Features.Investments.Models.Responses.TransactionDto>(3).ToList();
        autoMocker.GetMock<ITransactionService>().Setup(x => x.GetAllByUser(userId)).ReturnsAsync(transactions);

        // Act
        var result = await sut.GetTransactions(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(transactions);
        autoMocker.GetMock<ITransactionService>().Verify(x => x.GetAllByUser(userId), Times.Once);
    }

    [Fact]
    public async Task GetTransactions_WithNullUserId_ShouldReturnOkWithTransactions()
    {
        // Arrange
        var transactions = fixture.CreateMany<Babylon.Alfred.Api.Features.Investments.Models.Responses.TransactionDto>(2).ToList();
        autoMocker.GetMock<ITransactionService>().Setup(x => x.GetAllByUser(null)).ReturnsAsync(transactions);

        // Act
        var result = await sut.GetTransactions(null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(transactions);
        autoMocker.GetMock<ITransactionService>().Verify(x => x.GetAllByUser(null), Times.Once);
    }

    [Fact]
    public async Task GetTransactions_WithNoTransactions_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactions = new List<Babylon.Alfred.Api.Features.Investments.Models.Responses.TransactionDto>();
        autoMocker.GetMock<ITransactionService>().Setup(x => x.GetAllByUser(userId)).ReturnsAsync(transactions);

        // Act
        var result = await sut.GetTransactions(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(transactions);
        autoMocker.GetMock<ITransactionService>().Verify(x => x.GetAllByUser(userId), Times.Once);
    }

    [Fact]
    public async Task UpdateTransaction_WithValidRequest_ShouldReturnOkWithTransactionDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var updateRequest = fixture.Create<UpdateTransactionRequest>();
        var transactionDto = fixture.Create<Babylon.Alfred.Api.Features.Investments.Models.Responses.TransactionDto>();
        autoMocker.GetMock<ITransactionService>()
            .Setup(x => x.Update(userId, transactionId, updateRequest))
            .ReturnsAsync(transactionDto);

        // Act
        var result = await sut.UpdateTransaction(userId, transactionId, updateRequest);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(transactionDto);
        autoMocker.GetMock<ITransactionService>().Verify(x => x.Update(userId, transactionId, updateRequest), Times.Once);
    }

    [Fact]
    public async Task UpdateTransaction_WithTransactionNotFound_ShouldPropagateException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var updateRequest = fixture.Create<UpdateTransactionRequest>();
        autoMocker.GetMock<ITransactionService>()
            .Setup(x => x.Update(userId, transactionId, updateRequest))
            .ThrowsAsync(new InvalidOperationException($"Transaction {transactionId} not found for user {userId}"));

        // Act
        var act = async () => await sut.UpdateTransaction(userId, transactionId, updateRequest);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Transaction {transactionId} not found for user {userId}");
    }

    [Fact]
    public async Task DeleteTransaction_WithValidTransactionId_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        autoMocker.GetMock<ITransactionService>()
            .Setup(x => x.Delete(userId, transactionId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await sut.DeleteTransaction(userId, transactionId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = "Transaction deleted successfully" });
        autoMocker.GetMock<ITransactionService>().Verify(x => x.Delete(userId, transactionId), Times.Once);
    }

    [Fact]
    public async Task DeleteTransaction_WithTransactionNotFound_ShouldPropagateException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        autoMocker.GetMock<ITransactionService>()
            .Setup(x => x.Delete(userId, transactionId))
            .ThrowsAsync(new InvalidOperationException($"Transaction {transactionId} not found for user {userId}"));

        // Act
        var act = async () => await sut.DeleteTransaction(userId, transactionId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Transaction {transactionId} not found for user {userId}");
    }
}
