using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2.DataModel;
using Functional;
using Moq;
using Xunit;

namespace AccountProcessingLambda.Tests;

public class TransferHandlerTests
{
    private const string SampleJson = "{" +
        "\n  \"DebitedAccountId\": \"F78EFE64-DBC6-46F5-A8AB-E26C612CF4E4\"," +
        "\n  \"Beneficiary\": \"Me\"," +
        "\n  \"Iban\": \"DABF7DFA-A462-4D41-89D2-9B83FD5913AC\"," +
        "\n  \"Bic\": \"E213F36B-6258-40CD-A6C9-D883560AF72B\"," +
        "\n  \"Date\": \"2026-08-03\"," +
        "\n  \"Amount\": 26.0," +
        "\n  \"Reference\": \"Test\"" +
        "\n}";

    [Fact]
    public async Task Handle_NoTableNameProvider_CallsSaveAsyncWithoutConfig()
    {
        var mockContext = new Mock<IDynamoDBContext>();
        mockContext
            .Setup(m => m.SaveAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var handler = new AccountProcessingLambda.TransferHandler(mockContext.Object, () => null, () => DateTime.UtcNow);

        var request = new APIGatewayProxyRequest { Body = SampleJson };
        var lambdaContext = new TestLambdaContext();

        var response = await handler.Handle(request, lambdaContext);

        Assert.Equal(200, response.StatusCode);
        mockContext.Verify(m => m.SaveAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTableNameProvider_CallsSaveAsyncWithOperationConfig()
    {
        var mockContext = new Mock<IDynamoDBContext>();
        mockContext
            .Setup(m => m.SaveAsync(It.IsAny<object>(), It.IsAny<DynamoDBOperationConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var handler = new AccountProcessingLambda.TransferHandler(mockContext.Object, () => "AccountsTable", () => DateTime.UtcNow);

        var request = new APIGatewayProxyRequest { Body = SampleJson };
        var lambdaContext = new TestLambdaContext();

        var response = await handler.Handle(request, lambdaContext);

        Assert.Equal(200, response.StatusCode);
        mockContext.Verify(m => m.SaveAsync(It.IsAny<object>(), It.IsAny<DynamoDBOperationConfig>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Minimal ILambdaContext implementation for tests
    private class TestLambdaContext : ILambdaContext
    {
        public string AwsRequestId => Guid.NewGuid().ToString();
        public IClientContext ClientContext => null!;
        public string FunctionName => "TestFunction";
        public string FunctionVersion => "1";
        public ICognitoIdentity Identity => null!;
        public string InvokedFunctionArn => "arn:aws:lambda:local:0:function:TestFunction";
        public ILambdaLogger Logger => new TestLogger();
        public string LogGroupName => "test";
        public string LogStreamName => "test-stream";
        public int MemoryLimitInMB => 128;
        public TimeSpan RemainingTime => TimeSpan.FromMinutes(5);
    }

    private class TestLogger : ILambdaLogger
    {
        public void Log(string message) { }
        public void LogLine(string message) { }
    }
}
