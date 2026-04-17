// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Yukihana.Core.Debug;
using Yukihana.Core.Exceptions;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.Extensions.Primitives;

public static class OptionResultExtensions
{
    extension<T>(Option<T> option)
    {
        public T OrPanic(
            string context,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (option.IsSome)
                return option.Value;

            KernelPanic.Panic(
                string.IsNullOrWhiteSpace(context)
                    ? "Option was None."
                    : context,
                callerMemberName,
                callerFilePath,
                callerLineNumber);

            throw new UnreachableException();
        }

        public T OrThrow(
            string context)
        {
            if (option.IsSome)
                return option.Value;

            string message = string.IsNullOrWhiteSpace(context)
                ? "Option was None."
                : context;

            throw new OptionNoneException(message);
        }

        public Option<T> OnNone(Action handler)
        {
            if (option.IsNone)
                handler();

            return option;
        }

        public Result<T, TError> ToResult<TError>(TError noneError) =>
            option.IsSome
                ? Result<T, TError>.Success(option.Value)
                : Result<T, TError>.Failure(noneError);
    }

    extension<TValue, TError>(Result<TValue, TError> result)
    {
        public TValue OrPanic(
            string context,
            Func<TError, string>? errorFormatter = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (result.IsSuccess)
                return result.Value;

            string errorText = errorFormatter is not null
                ? errorFormatter(result.Error)
                : result.Error?.ToString() ?? "<null>";

            string reason = string.IsNullOrWhiteSpace(context)
                ? $"Operation failed: {errorText}"
                : $"{context}: {errorText}";

            KernelPanic.Panic(
                reason,
                callerMemberName,
                callerFilePath,
                callerLineNumber);

            throw new UnreachableException();
        }

        public TValue OrThrow(
            string context,
            Func<TError, string>? errorFormatter = null)
        {
            if (result.IsSuccess)
                return result.Value;

            string errorText = errorFormatter is not null
                ? errorFormatter(result.Error)
                : result.Error?.ToString() ?? "<null>";

            string reason = string.IsNullOrWhiteSpace(context)
                ? $"Operation failed: {errorText}"
                : $"{context}: {errorText}";

            throw new ResultException<TError>(reason, result.Error);
        }

        public Result<TValue, TError> OnFailure(Action<TError> handler)
        {
            if (result.IsFailure)
                handler(result.Error);

            return result;
        }

        public Option<TValue> ToOption() =>
            result.IsSuccess ? Option<TValue>.Some(result.Value) : Option<TValue>.None();
    }
}
