using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

    
// The function handler that will be called for each Lambda event
[Logging(LogEvent = true, Service = "AccountProcessing", LogLevel = LogLevel.Information)]
string Handler(APIGatewayProxyRequest request, ILambdaContext context)
{
    return request.Body.ToUpper();
}

// Build the Lambda runtime client passing in the handler to call for each
// event and the JSON serializer to use for translating Lambda JSON documents
// to .NET types.
await LambdaBootstrapBuilder.Create((Func<APIGatewayProxyRequest, ILambdaContext, string>?)Handler, new DefaultLambdaJsonSerializer())
        .Build()
        .RunAsync();