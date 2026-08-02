using Amazon.DynamoDBv2.DataModel;
using Functional;
using static Functional.F;

namespace AccountProcessingLambda;

public abstract record Command
{
    // Make timestamp init-only so Command instances are immutable once created
    public DateTime Timestamp { get; init; }

    public T WithTimestamp<T>(DateTime timestamp)
        where T : Command
    {
        // Use record 'with' expression to produce an immutable copy with updated timestamp
        return (T)(this with { Timestamp = timestamp });
    }
}

[DynamoDBTable("AccountsTable")]
public record MakeTransfer : Command
{
    [DynamoDBHashKey]
    public Guid DebitedAccountId { get; init; }

    // properties are init-only to enforce immutability
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

public record BookTransfer : MakeTransfer;
