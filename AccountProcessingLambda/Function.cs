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
using static Functional.F;
using System.Text.Json;
using static ActionResultFactory;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Unit = System.ValueTuple;


// Build the Lambda runtime client passing in the handler to call for each
// event and the JSON serializer to use for translating Lambda JSON documents
// to .NET types. Instantiate the handler with its dependencies here (not at file scope).
var dynamoDbContext = new DynamoDBContextBuilder()
    .WithDynamoDBClient(() => new AmazonDynamoDBClient())
    .Build();

var handlerInstance = new TransferHandler(dynamoDbContext, () => Environment.GetEnvironmentVariable("ACCOUNTS_TABLE_NAME"), () => DateTime.UtcNow);

await LambdaBootstrapBuilder.Create((Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>>)handlerInstance.Handle, new DefaultLambdaJsonSerializer())
        .Build()
        .RunAsync();


// Encapsulate handler dependencies so they can be injected rather than created at file scope
internal sealed class TransferHandler
{
    private readonly IDynamoDBContext _dynamoDbContext;
    private readonly Func<DateTime> _clock;
    private readonly Func<string?> _accountsTableNameProvider;

    public TransferHandler(IDynamoDBContext dynamoDbContext, Func<string?> accountsTableNameProvider, Func<DateTime> clock)
    {
        _dynamoDbContext = dynamoDbContext ?? throw new ArgumentNullException(nameof(dynamoDbContext));
        _accountsTableNameProvider = accountsTableNameProvider ?? throw new ArgumentNullException(nameof(accountsTableNameProvider));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    private Validation<BookTransfer> ParseRequest(string input)
    {
        try
        {
            var result = JsonSerializer.Deserialize<BookTransfer>(input);
            if (result is null)
                return Invalid<BookTransfer>("Request body could not be parsed as BookTransfer");

            // Use the functional smart constructor to validate required fields
            return MakeTransfer.CreateFrom(result);
        }
        catch (Exception ex)
        {
            return Invalid<BookTransfer>(ex.Message);
        }
    }

    [Logging(LogEvent = true, Service = "AccountProcessing", LogLevel = LogLevel.Information)]
    public async Task<APIGatewayProxyResponse> Handle(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation("Started");
        var validate = AccountProcessingLambda.Validation.DateNotPast(_clock);

        // Delay reading configuration and binding the save function until request time
        var accountsTableName = _accountsTableNameProvider();
        var save = string.IsNullOrEmpty(accountsTableName)
            ? _dynamoDbContext.WithContext<BookTransfer>()
            : _dynamoDbContext.WithContext<BookTransfer>(accountsTableName);

        var parsed = ParseRequest(request.Body);

        var result = await parsed.Match<Task<APIGatewayProxyResponse>>(
            Invalid: errs =>
            {
                // Log parse errors via Lambda Powertools logger
                context.Logger.LogError($"Request parse failed: {string.Join(", ", errs.Select(e => e.Message))}");
                return Task.FromResult(BadRequest(errs));
            },
            Valid: async command =>
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

        context.Logger.LogInformation($"Returning {result}");
        Logger.FlushBuffer();
        return result;
    }
}




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