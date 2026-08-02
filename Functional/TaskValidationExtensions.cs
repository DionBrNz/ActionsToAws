namespace Functional;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class TaskValidationExtensions
{
    // Allow calling MatchAsync directly on a Task<Validation<T>> where
    // Invalid is synchronous and Valid is asynchronous (matches Validation.MatchAsync).
    public static async Task<TR> MatchAsync<T, TR>(
        this Task<Validation<T>> validationTask,
        Func<IEnumerable<Error>, TR> Invalid,
        Func<T, Task<TR>> Valid)
    {
        var validation = await validationTask.ConfigureAwait(false);
        return await validation.MatchAsync(Invalid, Valid).ConfigureAwait(false);
    }

    // Fully asynchronous overload where both branches may be async.
    public static async Task<TR> MatchAsync<T, TR>(
        this Task<Validation<T>> validationTask,
        Func<IEnumerable<Error>, Task<TR>> InvalidAsync,
        Func<T, Task<TR>> ValidAsync)
    {
        var validation = await validationTask.ConfigureAwait(false);

        if (validation.IsValid)
            return await ValidAsync(validation.Value).ConfigureAwait(false);

        return await InvalidAsync(validation.Errors).ConfigureAwait(false);
    }
}
