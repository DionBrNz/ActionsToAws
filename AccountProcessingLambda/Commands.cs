using Amazon.DynamoDBv2.DataModel;
using Functional;
using static Functional.F;

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

    // make properties init-only and provide defaults to satisfy nullable checking
    public string Beneficiary { get; init; } = default!;
    public string Iban { get; init; } = default!;
    public string Bic { get; init; } = default!;

    public DateTime Date { get; init; }
    public decimal Amount { get; init; }
    public string Reference { get; init; } = default!;

    // Functional smart constructor that validates required fields and returns Validation<BookTransfer>
    public static Validation<BookTransfer> CreateFrom(MakeTransfer m)
    {
        var errs = new List<Error>();

        if (m.DebitedAccountId == Guid.Empty) errs.Add("DebitedAccountId is required");
        if (string.IsNullOrWhiteSpace(m.Beneficiary)) errs.Add("Beneficiary is required");
        if (string.IsNullOrWhiteSpace(m.Iban)) errs.Add("Iban is required");
        if (string.IsNullOrWhiteSpace(m.Bic)) errs.Add("Bic is required");
        if (m.Amount <= 0) errs.Add("Amount must be greater than zero");

        return errs.Any()
            ? Invalid<BookTransfer>(errs)
            : Valid<BookTransfer>(new BookTransfer
            {
                DebitedAccountId = m.DebitedAccountId,
                Beneficiary = m.Beneficiary,
                Iban = m.Iban,
                Bic = m.Bic,
                Date = m.Date,
                Amount = m.Amount,
                Reference = m.Reference
            });
    }
}

public class BookTransfer : MakeTransfer { }