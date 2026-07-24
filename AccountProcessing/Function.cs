using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Tracing;
using Amazon.Lambda.APIGatewayEvents;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace UpperCase;

/// <summary>
/// Learn more about Powertools for AWS Lambda (.NET) at https://awslabs.github.io/aws-lambda-powertools-dotnet/
/// Powertools for AWS Lambda (.NET) Logging: https://awslabs.github.io/aws-lambda-powertools-dotnet/core/logging/
/// Powertools for AWS Lambda (.NET) Tracing: https://awslabs.github.io/aws-lambda-powertools-dotnet/core/tracing/
/// Powertools for AWS Lambda (.NET) Metrics: https://awslabs.github.io/aws-lambda-powertools-dotnet/core/metrics/
/// Metrics Namespace and Service can be defined through Environment Variables https://awslabs.github.io/aws-lambda-powertools-dotnet/core/metrics/#getting-started
/// </summary>
public class Function
{
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    [Logging(LogEvent = true, Service = "UpperCase", LogLevel = LogLevel.Information)]
    [Tracing]
    [Metrics(Namespace = "UpperCase", Service = "UpperCase", RaiseOnEmptyMetrics = true)]
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {

        var body = request.Body;
        Metrics.AddMetric("Handler_called", 1, MetricUnit.Count);
        var upperCaseString = UpperCaseString(body);

        Logger.LogInformation($"Uppercase of '{body}' is {upperCaseString}");

        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = upperCaseString
        };
    }

    [Metrics(CaptureColdStart = true)]
    [Tracing(SegmentName = "UpperCaseString Method")]
    private static string UpperCaseString(string input)
    {
        try
        {
            Metrics.AddMetric("UpperCaseString_Invocations", 1, MetricUnit.Count);


            return input.ToUpper();

        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }
}
