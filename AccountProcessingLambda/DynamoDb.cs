using Amazon.DynamoDBv2.DataModel;
using AWS.Lambda.Powertools.Logging;
using Functional;
using System.Threading;
using Unit = System.ValueTuple;


namespace AccountProcessingLambda;


public static class DynamoDb
{
    public static Func<IDynamoDBContext, string, string, Task<Exceptional<Unit>>> TryFetch =>
        async (client, tableName, key) =>
        {
            try
            {
               await client.LoadAsync(key).ConfigureAwait(false);
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
            await context.SaveAsync(entity, CancellationToken.None).ConfigureAwait(false);
            return new Unit();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "error");
            return ex;
        }
    }

    public static async Task<Exceptional<Unit>> TryExecuteWithTable<T>(IDynamoDBContext context, T entity, string tableName)
        where T : Command
    {
        try
        {
            Logger.LogInformation("About to save to table {Table}", tableName);
            // apply timestamp immutably using the domain Command helper
            var toSave = entity.WithTimestamp<T>(DateTime.UtcNow);
            var config = new DynamoDBOperationConfig { OverrideTableName = tableName };
            await context.SaveAsync(toSave, config, CancellationToken.None).ConfigureAwait(false);
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

    public static Func<T, Task<Exceptional<Unit>>> WithContext<T>(this IDynamoDBContext context, string tableName)
        where T : Command
        => entity => TryExecuteWithTable(context, entity, tableName);
}