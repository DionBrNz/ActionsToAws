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

    // Use a generic method so the DynamoDBContext SaveAsync receives the concrete
    // type rather than System.Object which causes the SDK to try to map "object".
    public static async Task<Exceptional<Unit>> TryExecute<T>(IDynamoDBContext context, T entity)
    {
        try
        {
            Logger.LogInformation("About to save");
            await context.SaveAsync(entity).ConfigureAwait(false);
            return new Unit();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "error");
            return ex;
        }
    }

    // Functional helper: bind a concrete IDynamoDBContext to produce a function that
    // saves entities of type T. This lets callers compose the save function in a
    // functional / curried style: var save = dbContext.WithContext<BookTransfer>();
    public static Func<T, Task<Exceptional<Unit>>> WithContext<T>(this IDynamoDBContext context)
        => entity => TryExecute(context, entity);
}