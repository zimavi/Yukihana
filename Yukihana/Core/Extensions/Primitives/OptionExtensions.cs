// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.Extensions.Primitives;

public static class OptionExtensions
{
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> items, Func<T, bool> predicate) =>
        items
            .Where(predicate)
            .Select(Option<T>.Some)
            .DefaultIfEmpty(Option<T>.None())
            .First();
    
    public static Option<TResult> Select<T, TResult>(this Option<T> obj, Func<T, TResult> map) => obj.Map(map);

    public static Option<T> Where<T>(this Option<T> obj, Func<T, bool> predicate) =>
        obj.Bind(content => predicate(content) ? obj : Option<T>.None());

    public static Option<TResult> SelectMany<T, TR, TResult>(
        this Option<T> obj, Func<T, Option<TR>> bind, Func<T, TR, TResult> map) =>
        obj.Bind(original => bind(original).Map(result => map(original, result)));
}