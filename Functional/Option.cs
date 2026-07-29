namespace Functional
{
    using Functional;
    using static F;
    using Unit = System.ValueTuple;
    public static partial class F
    {
        public static Option<T> Some<T>(T value) => new Option.Some<T>(value); // wrap the given value into a Some
        public static Option.None None => Option.None.Default;  // the None value
    }

    namespace Option
    {
        public struct None
        {
            internal static readonly None Default = new None();
        }

        public struct Some<T>
        {
            internal T Value { get; }

            internal Some(T value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value)
                       , "Cannot wrap a null value in a 'Some'; use 'None' instead");
                Value = value;
            }
        }
    }



    public struct Option<T>
    {
        readonly bool isSome;
        readonly T value;

        private Option(T value)
        {
            isSome = true;
            this.value = value;
        }

        public IEnumerable<T> AsEnumerable()
        {
            if (isSome) yield return value;
        }

        public static implicit operator Option<T>(Option.None _) => new Option<T>();

        public static implicit operator Option<T>(Option.Some<T> some) => new Option<T>(some.Value);

        public static implicit operator Option<T>(T value)
         => value == null ? None : Some(value);

        public R Match<R>(Func<R> None, Func<T, R> Some) => isSome ? Some(value) : None();
    }

    public static class OptionExt
    {
        public static Option<R> Apply<T, R>
         (this Option<Func<T, R>> @this, Option<T> arg)
         => @this.Match(
            () => None,
            (func) => arg.Match(
               () => None,
               (val) => Some(func(val))));

        public static Option<R> Bind<T, R>
        (this Option<T> optT, Func<T, Option<R>> f)
         => optT.Match(
            () => None,
            (t) => f(t));

        public static IEnumerable<R> Bind<T, R>
           (this Option<T> @this, Func<T, IEnumerable<R>> func)
            => @this.AsEnumerable().Bind(func);

        public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> @this, Func<T, IEnumerable<R>> func)
        => @this.SelectMany(func);

        public static Option<R> Map<T, R>
         (this Option<T> optT, Func<T, R> f)
         => optT.Match(
            () => None,
            (t) => Some(f(t)));

        public static Option<Unit> ForEach<T>(this Option<T> @this, Action<T> action)
         => Map(@this, action.ToFunc());

        public static Option<T> Where<T>
         (this Option<T> optT, Func<T, bool> predicate)
         => optT.Match(
            () => None,
            (t) => predicate(t) ? optT : None);
    }


}
