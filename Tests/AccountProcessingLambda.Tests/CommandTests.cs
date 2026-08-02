using System;
using Xunit;

namespace AccountProcessingLambda.Tests;

public class CommandTests
{
    [Fact]
    public void WithTimestamp_ReturnsNewInstanceWithUpdatedTimestamp()
    {
        var original = new AccountProcessingLambda.MakeTransfer
        {
            DebitedAccountId = Guid.NewGuid(),
            Beneficiary = "B",
            Iban = "I",
            Bic = "C",
            Date = DateTime.UtcNow,
            Amount = 1m,
            Reference = "r"
        };

        var now = new DateTime(2026, 8, 4);
        var updated = original.WithTimestamp<AccountProcessingLambda.MakeTransfer>(now);

        Assert.NotNull(updated);
        Assert.Equal(now, updated.Timestamp);
        // original should remain unchanged (records are immutable)
        Assert.NotEqual(now, original.Timestamp);
        // other properties should be equal
        Assert.Equal(original.DebitedAccountId, updated.DebitedAccountId);
        Assert.Equal(original.Beneficiary, updated.Beneficiary);
    }
}
