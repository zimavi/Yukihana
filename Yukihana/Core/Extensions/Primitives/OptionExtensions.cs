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
    
    public static Option<R> Select<T, R>(this Option<T> obj, Func<T, R> map) => obj.Map(map);

    public static Option<T> Where<T>(this Option<T> obj, Func<T, bool> predicate) =>
        obj.Bind(content => predicate(content) ? obj : Option<T>.None());

    public static Option<TResult> SelectMany<T, R, TResult>(
        this Option<T> obj, Func<T, Option<R>> bind, Func<T, R, TResult> map) =>
        obj.Bind(original => bind(original).Map(result => map(original, result)));
}