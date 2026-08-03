using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Functional;
using Moq;
using Xunit;

namespace AccountProcessingLambda.Tests;

public class DynamoDbTests
{
    [Fact]
    public async Task TryExecute_CallsSaveAsync_ReturnsSuccess()
    {
        var mockContext = new Mock<IDynamoDBContext>();
        mockContext
            .Setup(m => m.SaveAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var entity = new AccountProcessingLambda.MakeTransfer
        {
            DebitedAccountId = Guid.NewGuid(),
            Beneficiary = "B",
            Iban = "I",
            Bic = "C",
            Date = DateTime.UtcNow,
            Amount = 1m,
            Reference = "r"
        };

        var result = await AccountProcessingLambda.DynamoDb.TryExecute(mockContext.Object, entity);

        Assert.True(result.Success);
        mockContext.Verify(m => m.SaveAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryExecuteWithTable_CallsSaveAsyncWithConfig_ReturnsSuccess()
    {
        var mockContext = new Mock<IDynamoDBContext>();
        DynamoDBOperationConfig? captured = null;
        mockContext
            .Setup(m => m.SaveAsync(It.IsAny<object>(), It.IsAny<DynamoDBOperationConfig>(), It.IsAny<CancellationToken>()))
            .Callback<object, DynamoDBOperationConfig, CancellationToken>((o, cfg, tok) => captured = cfg)
            .Returns(Task.CompletedTask)
            .Verifiable();

        var entity = new AccountProcessingLambda.MakeTransfer
        {
            DebitedAccountId = Guid.NewGuid(),
            Beneficiary = "B",
            Iban = "I",
            Bic = "C",
            Date = DateTime.UtcNow,
            Amount = 1m,
            Reference = "r"
        };

        var result = await AccountProcessingLambda.DynamoDb.TryExecuteWithTable(mockContext.Object, entity, "AccountsTable");

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal("AccountsTable", captured!.OverrideTableName);
        mockContext.Verify(m => m.SaveAsync(It.IsAny<object>(), It.IsAny<DynamoDBOperationConfig>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryFetch_CallsLoadAsync_ReturnsSuccess()
    {
        var mockContext = new Mock<IDynamoDBContext>();
        mockContext
            .Setup(m => m.LoadAsync<object>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object())
            .Verifiable();

        var func = AccountProcessingLambda.DynamoDb.TryFetch;

        var result = await func(mockContext.Object, "AccountsTable", "some-key");

        Assert.True(result.Success);
        mockContext.Verify(m => m.LoadAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
