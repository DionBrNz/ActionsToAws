using Amazon.DynamoDBv2.DataModel;

namespace AccountProcessingLambda;

public abstract class Command
{
    public DateTime Timestamp { get; set; }

    public T WithTimestamp<T>(DateTime timestamp)
        where T : Command
    {
        T result = (T)MemberwiseClone();
        result.Timestamp = timestamp;
        return result;
    }
}

[DynamoDBTable("AccountsTable")]
public class MakeTransfer : Command
{
    [DynamoDBHashKey]
    public Guid DebitedAccountId { get; set; }

    public string Beneficiary { get; set; }
    public string Iban { get; set; }
    public string Bic { get; set; }

    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; }
}

public class BookTransfer : MakeTransfer { }