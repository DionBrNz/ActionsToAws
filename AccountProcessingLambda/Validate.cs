using AccountProcessingLambda.Domain;
using Functional;
using static Functional.F;

namespace AccountProcessingLambda;

public delegate Validation<T> Validator<T>(T t);

public static class Validation
{
    public static Validator<BookTransfer> DateNotPast(Func<DateTime> clock)
        => cmd
            => cmd.Date.Date < clock().Date
                ? Errors.TransferDateIsPast
                : Valid(cmd);
}