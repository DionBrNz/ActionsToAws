using Amazon.DynamoDBv2.DataModel;
using AWS.Lambda.Powertools.Logging;
using Functional;
using Unit = System.ValueTuple;


namespace AccountProcessingLambda;


public static class DynamoDb
{
    public static Func<IDynamoDBContext, string, string, Task<Exceptional<Unit>>> TryFetch =>
        async (client, tableName, key) =>
        {
            try
            {

               await client.LoadAsync(key);
            }
            catch (Exception ex)
            {
                return ex;
            }

            return new Unit();
        };

    public static Func<IDynamoDBContext, object, Task<Exceptional<Unit>>> TryExecute =>
        async (context, entity) =>
        {
            try
            {
                Logger.LogInformation("About to save");
                await context.SaveAsync(entity);
                return new Unit();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "error");
                return ex;
            }
        };
}