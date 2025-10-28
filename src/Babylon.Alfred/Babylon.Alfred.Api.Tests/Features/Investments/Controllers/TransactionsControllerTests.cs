using AutoFixture;
using AutoFixture.AutoMoq;
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

        fixture.Register(() => new DateOnly(2025, 10, 28));
        sut = autoMocker.CreateInstance<TransactionsController>();
    }

    [Fact]
    public async Task CreateTransactionAsync_WithValidRequest_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var request = fixture.Create<CreateTransactionRequest>();
        var transaction = fixture.Create<Transaction>();
        autoMocker.GetMock<ITransactionService>().Setup(x => x.Create(request)).ReturnsAsync(transaction);

        // Act
        var result = await sut.CreateTransactionAsync(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = "Successfully stored the transaction" });
        autoMocker.GetMock<ITransactionService>().Verify(x => x.Create(request), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionsBulkAsync_WithValidRequests_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var requests = fixture.CreateMany<CreateTransactionRequest>(5).ToList();
        var transactions = fixture.CreateMany<Transaction>(5).ToList();
        autoMocker.GetMock<ITransactionService>().Setup(x => x.CreateBulk(requests)).ReturnsAsync(transactions);

        // Act
        var result = await sut.CreateTransactionsBulkAsync(requests);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = $"Successfully stored {transactions.Count} transactions" });
        autoMocker.GetMock<ITransactionService>().Verify(x => x.CreateBulk(requests), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionsBulkAsync_WithEmptyList_ShouldReturnOkWithZeroCount()
    {
        // Arrange
        var requests = new List<CreateTransactionRequest>();
        var transactions = new List<Transaction>();
        autoMocker.GetMock<ITransactionService>().Setup(x => x.CreateBulk(requests)).ReturnsAsync(transactions);

        // Act
        var result = await sut.CreateTransactionsBulkAsync(requests);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { message = "Successfully stored 0 transactions" });
        autoMocker.GetMock<ITransactionService>().Verify(x => x.CreateBulk(requests), Times.Once);
    }
}
