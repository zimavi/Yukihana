// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.Extensions.Primitives;

public static partial class OptionExtensions
{

    extension<T>(Option<T> option)
    {
        public Option<TResult> Select<TResult>(Func<T, TResult> map) => option.Map(map);

        public Option<T> Where(Func<T, bool> predicate) =>
            option.Bind(content => predicate(content) ? option : Option<T>.None());

        public Option<TResult> SelectMany<TR, TResult>(
            Func<T, Option<TR>> bind, Func<T, TR, TResult> map) =>
            option.Bind(original => bind(original).Map(result => map(original, result)));

        public bool IsSomeAnd(Func<T, bool> predicate) =>
            option.IsSome && predicate(option.Value);

        public Option<T> Filter(Func<T, bool> predicate) =>
            option.IsSome && predicate(option.Value)
                ? option
                : Option<T>.None();

        public Option<T> OnSome(Action<T> action)
        {
            if (option.IsSome)
            {
                action(option.Value);
            }

            return option;
        }

        public Option<T> OrElse(Func<Option<T>> fallback) =>
            option.IsSome ? option : fallback();

        public Option<T> Or(Option<T> fallback) =>
            option.IsSome ? option : fallback;

        public Option<(T, TOther)> Zip<TOther>(Option<TOther> other) =>
            option.IsSome && other.IsSome
                ? Option<(T, TOther)>.Some((option.Value, other.Value))
                : Option<(T, TOther)>.None();

        public Option<T> Flatten() =>
            option.IsSome && option.Value is Option<T> inner
                ? inner
                : option;

        public T? ToNullable() =>
            option.IsSome ? option.Value : default;

        public IEnumerable<T> AsEnumerable()
        {
            if (option.IsSome)
            {
                yield return option.Value;
            }
        }
    }
}
