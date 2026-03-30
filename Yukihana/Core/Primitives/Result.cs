// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Primitives;

/// <summary>
/// Represents the outcome of an operation that can either succeed with a value of type <typeparamref name="TValue"/>
/// or fail with an error of type <typeparamref name="TError"/>
/// </summary>
/// <typeparam name="TValue">The type of successful result value.</typeparam>
/// <typeparam name="TError">The type of failure error value.</typeparam>
public readonly struct Result<TValue, TError> : IEquatable<Result<TValue, TError>>
{
#region Properties & fields

    private readonly TValue? _value;
    private readonly TError? _error;

    /// <summary>
    /// Gets a value indicating whether the result represents success.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result represents failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the successful value, or throws if the result is a failure.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("Result is a failure.");

    /// <summary>
    /// Gets the error value, or throws if the result is a success.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a success.</exception>
    public TError Error => IsFailure ? _error! : throw new InvalidOperationException("Result is a success.");

#endregion

#region Constructor & factory methods

    private Result(TValue? value, TError? error, bool isSuccess)
    {
        _value = value;
        _error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// Creates a successful result containing the specified value.
    /// </summary>
    public static Result<TValue, TError> Success(TValue value) => new(value, error: default, isSuccess: true);

    /// <summary>
    /// Creates a failed result containing the specified error.
    /// </summary>
    public static Result<TValue, TError> Failure(TError error) => new(value: default, error, isSuccess: false);

#endregion

#region Methods

    /// <summary>
    /// Attempts to retrieve the successful value.
    /// </summary>
    /// <param name="value">The contained value if the result is successful; otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if the result is successful; otherwise <c>false</c>.</returns>
    public bool TryGetValue(out TValue? value)
    {
        value = _value;
        return IsSuccess;
    }

    /// <summary>
    /// Attempts to retrieve the error value.
    /// </summary>
    /// <param name="error">The contained error if the result is a failure; otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if the result is a failure; otherwise <c>false</c>.</returns>
    public bool TryGetError(out TError? error)
    {
        error = _error;
        return IsFailure;
    }

    /// <summary>
    /// Returns the successful value, or throws if the result is a failure.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public TValue Unwrap() => Value;

    /// <summary>
    /// Returns the error value, or throws if the result is a success.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a success.</exception>
    public TError UnwrapError() => Error;

    /// <summary>
    /// Returns the successful value if present; otherwise returns the specified default value.
    /// </summary>
    public TValue UnwrapOr(TValue defaultValue) => IsSuccess ? _value! : defaultValue;

    /// <summary>
    /// Returns the successful value if present; otherwise computes one from the error value.
    /// </summary>
    public TValue UnwrapOrElse(Func<TError, TValue> errorHandler) => IsSuccess ? _value! : errorHandler(_error!);

    /// <summary>
    /// Transforms the successful value while preserving the error type.
    /// </summary>
    public Result<TResult, TError> Map<TResult>(Func<TValue, TResult> selector) =>
        IsSuccess
            ? Result<TResult, TError>.Success(selector(_value!))
            : Result<TResult, TError>.Failure(_error!);

    /// <summary>
    /// Transforms the error value while preserving the success type.
    /// </summary>
    public Result<TValue, TResult> MapError<TResult>(Func<TError, TResult> selector) =>
        IsSuccess
            ? Result<TValue, TResult>.Success(_value!)
            : Result<TValue, TResult>.Failure(selector(_error!));

    /// <summary>
    /// Chains another result-producing function if this result is successful.
    /// </summary>
    public Result<TResult, TError> Bind<TResult>(Func<TValue, Result<TResult, TError>> binder) =>
        IsSuccess
            ? binder(_value!)
            : Result<TResult, TError>.Failure(_error!);

    /// <summary>
    /// Executes an action for a successful result and returns this result unchanged.
    /// </summary>
    public Result<TValue, TError> Tap(Action<TValue> action)
    {
        if (IsSuccess)
            action(_value!);
        return this;
    }

    /// <summary>
    /// Executes an action for a failed result and returns this result unchanged.
    /// </summary>
    public Result<TValue, TError> TapError(Action<TError> action)
    {
        if (IsFailure)
            action(_error!);
        return this;
    }

    /// <summary>
    /// Validates the successful value and converts it to a failure if the predicate returns <c>false</c>.
    /// </summary>
    public Result<TValue, TError> Ensure(Func<TValue, bool> predicate, TError error) =>
        IsSuccess && !predicate(_value!)
            ? Failure(error)
            : this;

    /// <summary>
    /// Matches the result state and returns a value from the corresponding branch.
    /// </summary>
    public TResult Match<TResult>(Func<TValue, TResult> success, Func<TError, TResult> failure) =>
        IsSuccess ? success(_value!) : failure(_error!);

    /// <summary>
    /// Matches the result state and executes the corresponding action.
    /// </summary>
    public void Switch(Action<TValue> success, Action<TError> failure)
    {
        if(IsSuccess)
            success(_value!);
        else
            failure(_error!);
    }

    /// <summary>
    /// Deconstructs the result into its state, value, and error.
    /// </summary>
    public void Deconstruct(out bool isSuccess, out TValue? value, out TError? error)
    {
        isSuccess = IsSuccess;
        value = _value;
        error = _error;
    }

#endregion

#region Object overrides & interface implementations

    public override string ToString() => IsSuccess ? $"Success({_value})" : $"Failure({_error})";

    public bool Equals(Result<TValue, TError> other)
    {
        if (IsSuccess != other.IsSuccess)
            return false;

        return IsSuccess 
            ? EqualityComparer<TValue>.Default.Equals(_value, other._value)
            : EqualityComparer<TError>.Default.Equals(_error, other._error);
    }

    public override bool Equals(object? obj) => obj is Result<TValue, TError> other && Equals(other);

    public override int GetHashCode() =>
        IsSuccess
            ? EqualityComparer<TValue>.Default.GetHashCode(_value!)
            : EqualityComparer<TError>.Default.GetHashCode(_error!);

#endregion

#region Operator overloads

    public static bool operator ==(Result<TValue, TError>? left, Result<TValue, TError>? right) => 
        EqualityComparer<Result<TValue, TError>?>.Default.Equals(left, right);
    public static bool operator !=(Result<TValue, TError>? left, Result<TValue, TError>? right) => !(left == right);

    public static bool operator true(Result<TValue, TError>? self) => self is not null && self.Value.IsSuccess;
    public static bool operator false(Result<TValue, TError>? self) => self is null || self.Value.IsFailure;

    public static implicit operator Result<TValue, TError>(TValue value) => Success(value);

#endregion
}