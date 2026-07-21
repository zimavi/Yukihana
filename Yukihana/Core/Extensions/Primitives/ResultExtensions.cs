// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.Extensions.Primitives;

public static partial class ResultExtensions
{
    extension<TValue, TError>(Result<TValue, TError> result)
    {
        public bool IsSuccessAnd(Func<TValue, bool> predicate) =>
            result.IsSuccess && predicate(result.Value);

        public Result<TValue, TError> Recover(Func<TError, TValue> handler) =>
            result.IsSuccess
                ? result
                : Result<TValue, TError>.Success(handler(result.Error));

        public Result<TValue, TError> RecoverWith(Func<TError, Result<TValue, TError>> handler) =>
            result.IsSuccess ? result : handler(result.Error);

        public Result<TValue, TError> Ensure(Func<TValue, bool> predicate, Func<TError> errorFactory) =>
            result.IsSuccess && !predicate(result.Value)
                ? Result<TValue, TError>.Failure(errorFactory())
                : result;

        public Result<(TValue, TOther), TError> Zip<TOther>(Result<TOther, TError> other) =>
            result.IsSuccess && other.IsSuccess
                ? Result<(TValue, TOther), TError>.Success((result.Value, other.Value))
                : result.IsFailure
                    ? Result<(TValue, TOther), TError>.Failure(result.Error)
                    : Result<(TValue, TOther), TError>.Failure(other.Error);

        public Option<TValue> Ok() =>
            result.IsSuccess ? Option<TValue>.Some(result.Value) : Option<TValue>.None();

        public Option<TError> Err() =>
            result.IsFailure ? Option<TError>.Some(result.Error) : Option<TError>.None();

        public Result<TError, TValue> Swap() =>
            result.IsSuccess
                ? Result<TError, TValue>.Failure(result.Value)
                : Result<TError, TValue>.Success(result.Error);

        public Result<TValue, TError> TapBoth(Action<TValue> success, Action<TError> failure)
        {
            if (result.IsSuccess)
            {
                success(result.Value);
            }
            else
            {
                failure(result.Error);
            }

            return result;
        }
    }
}
