// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.Extensions.Primitives;

public static partial class EnumerableExtensions
{
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> items, Func<T, bool> predicate) =>
        items
            .Where(predicate)
            .Select(Option<T>.Some)
            .DefaultIfEmpty(Option<T>.None())
            .First();

    public static Option<T[]> Collect<T>(this IEnumerable<Option<T>> source)
    {
        var list = new List<T>();

        foreach (Option<T> item in source)
        {
            if (item.IsNone)
                return Option<T[]>.None();

            list.Add(item.Value);
        }

        return Option<T[]>.Some([.. list]);
    }

    public static Result<T[], TError> Collect<T, TError>(this IEnumerable<Result<T, TError>> source)
    {
        var list = new List<T>();

        foreach (Result<T, TError> item in source)
        {
            if (item.IsFailure)
                return Result<T[], TError>.Failure(item.Error);

            list.Add(item.Value);
        }

        return Result<T[], TError>.Success(list.ToArray());
    }

    public static Option<T> FirstSome<T>(this IEnumerable<Option<T>> source)
    {
        foreach (Option<T> item in source)
        {
            if (item.IsSome)
                return item;
        }

        return Option<T>.None();
    }
}
