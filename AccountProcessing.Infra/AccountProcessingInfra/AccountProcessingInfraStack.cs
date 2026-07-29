using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.APIGateway;
using Constructs;

namespace AccountProcessingInfra
{
    public class AccountProcessingInfraStack : Stack
    {
        public AccountProcessingInfraStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // Define the Lambda function
            var function = new Function(this, "UpperCase", new FunctionProps
            {
                Handler = "UpperCase::UpperCase.Function::FunctionHandler", 
                FunctionName = "UpperCase",
                Runtime = Runtime.DOTNET_10,
                Architecture = Architecture.ARM_64,
                Code = Code.FromAsset("AccountProcessing\\bin\\Debug\\net10.0"),
                Timeout = Duration.Seconds(10),
                LoggingFormat = LoggingFormat.JSON
            });
            // Explicitly define the log group for the Lambda function
            var logGroup = new LogGroup(this, "UpperCaseLogGroup", new LogGroupProps
            {
                LogGroupName = $"/aws/lambda/uppercase",
                Retention = RetentionDays.ONE_DAY,
                RemovalPolicy = RemovalPolicy.DESTROY // Automatically delete the log group when the stack is destroyed
            });

            logGroup.GrantWrite(function);

            // Define the API Gateway
            var api = new RestApi(this, "MyApiGateway", new RestApiProps
            {
                RestApiName = "AccountProcessingAPI",
                Description = "API Gateway for Account Processing"
            });
            // Add a resource (e.g., /process)
            var processResource = api.Root.AddResource("process");
            // Add a POST method to the resource and integrate it with the Lambda function
            processResource.AddMethod("POST", new LambdaIntegration(function), new MethodOptions
            {
                // Optional: Add authorization or other method options here
            });
        }
    }
}