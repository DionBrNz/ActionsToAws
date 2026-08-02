using AccountProcessingLambda;
using AccountProcessingLambda.Domain;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Logging;
using Functional;
using System.Linq;
using System.Text.Json;
using System.Xml.Schema;
using static ActionResultFactory;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Unit = System.ValueTuple;


IDynamoDBContext context = new DynamoDBContextBuilder().WithDynamoDBClient(() => new AmazonDynamoDBClient()).Build();

Validator<BookTransfer> validate = AccountProcessingLambda.Validation.DateNotPast(() => DateTime.UtcNow);
Func<BookTransfer, Task<Exceptional<Unit>>> save = command => DynamoDb.TryExecute(context, command);

Exceptional<BookTransfer> ParseRequest(string input)
{
    try
    {
        var result = JsonSerializer.Deserialize<BookTransfer>(input);
        if (result is null)
            return new Exception("Request body could not be parsed as BookTransfer");

        return Exceptional.Of<BookTransfer>(result);
    }
    catch (Exception ex)
    {
        return Exceptional.Of<BookTransfer>(ex);
    }
}

// The function handler that will be called for each Lambda event
[Logging(LogEvent = true, Service = "AccountProcessing", LogLevel = LogLevel.Information)]
async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
{
    var parsed = ParseRequest(request.Body);

    var result = await parsed.Match<Task<APIGatewayProxyResponse>>(
        Exception: ex =>
        {
            // Log parse errors via Lambda Powertools logger
            context.Logger.LogError($"Request parse failed: {ex}");
            return Task.FromResult(BadRequest(new { Message = ex.Message }));
        },
        Success: async command =>
        {
            return await validate(command)
                .MapAsync(save)
                .MatchAsync(
                    Invalid: errs =>
                    {
                        // Log validation failures via Lambda Powertools logger
                        context.Logger.LogWarning($"Validation failed: {string.Join(", ", errs.Select(e => e.Message))}");
                        return BadRequest(errs);
                    },
                    Valid: r => Task.FromResult(r.Match<APIGatewayProxyResponse>(
                        Exception: ex =>
                        {
                            // Log persistence exceptions via Lambda Powertools logger
                            context.Logger.LogError($"Save failed: {ex}");
                            return InternalServerError(Errors.UnexpectedError);
                        },
                        Success: _ => Ok())));
        });

    return result;
}




// Build the Lambda runtime client passing in the handler to call for each
// event and the JSON serializer to use for translating Lambda JSON documents
// to .NET types.
await LambdaBootstrapBuilder.Create((Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>>
        ?)Handler, new DefaultLambdaJsonSerializer())
        .Build()
        .RunAsync();




static class ActionResultFactory
{
    public static APIGatewayProxyResponse Ok() => new() { StatusCode = 200 };
    public static APIGatewayProxyResponse Ok(object value) => new() { Body = JsonSerializer.Serialize(value), StatusCode = 200 };
    public static APIGatewayProxyResponse BadRequest(object error) => new() { Body = JsonSerializer.Serialize(error), StatusCode = 400 };
    public static APIGatewayProxyResponse InternalServerError(object value)
    {
        return new APIGatewayProxyResponse
        {
            Body = JsonSerializer.Serialize(value),
            StatusCode = 500
        };
    }
}