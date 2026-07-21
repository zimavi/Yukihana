// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Yukihana.Core.Primitives;

/// <summary>
/// Represents an optional value: either <c>Some</c> containing a value of type <typeparamref name="T"/>,
/// or <c>None</c> representing the absence of a value.
/// </summary>
/// <typeparam name="T">The type of contained value.</typeparam>
public readonly struct Option<T> : IEquatable<Option<T>>, IEnumerable<T>, IDisposable
{
    #region Properties & fields
    private readonly T? _value;

    /// <summary>
    /// Gets a value indicating whether the option contains a value.
    /// </summary>
    public bool IsSome { get; }

    /// <summary>
    /// Gets a value indicating whether the option is empty.
    /// </summary>
    public bool IsNone => !IsSome;

    /// <summary>
    /// Gets the contained value if preset; otherwise throws an exception.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the option is empty.</exception>
    public T Value => IsSome ? _value! : throw new InvalidOperationException("Option has no value.");

    #endregion

    #region Constructor & factory methods

    private Option(T? value, bool isSome)
    {
        _value = value;
        IsSome = isSome;
    }

    /// <summary>
    /// Creates an option containg a value.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    public static Option<T> Some(T value) => new(value, true);

    /// <summary>
    /// Creates an empty option.
    /// </summary>
    public static Option<T> None() => new(default, false);

    public static Option<T> From(T? value) => value is null ? None() : Some(value);

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (IsSome)
        {
            DisposeIfNeeded(_value);
        }
    }

    private static void DisposeIfNeeded(T? value)
    {
        if (value is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Retuns contained value if present; otherwise throws an exception.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the option is empty.</exception>
    public T Unwrap() => Value;

    /// <summary>
    /// Returns the contained value if present; otherwise returns the specified default value.
    /// </summary>
    /// <param name="defaultValue">The fallback value.</param>
    public T UnwrapOr(T defaultValue) => IsSome ? _value! : defaultValue;

    /// <summary>
    /// Returns the contained value if present; otherwise computes it using the provided factory function.
    /// </summary>
    /// <param name="factory">A function to produce a fallback value.</param>
    public T UnwrapOrElse(Func<T> factory) => IsSome ? _value! : factory();

    /// <summary>
    /// Transforms the contained value using the specified selector function if present.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="selector">The transformation function.</param>
    public Option<TResult> Map<TResult>(Func<T, TResult> selector) =>
        IsSome ? Option<TResult>.Some(selector(_value!)) : Option<TResult>.None();

    /// <summary>
    /// Chains another optional-producing function, flattening the result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="binder">A function returning another option.</param>
    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> binder) =>
        IsSome ? binder(_value!) : Option<TResult>.None();

    /// <summary>
    /// Matches the option state and executes the corresponding function.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="some">Function to execute if value is present.</param>
    /// <param name="none">Function to execute if value is absent.</param>
    public TResult Map<TResult>(Func<T, TResult> some, Func<TResult> none) =>
        IsSome ? some(_value!) : none();

    /// <summary>
    /// Executes one of two actions depending on whether a value is present.
    /// </summary>
    /// <param name="some">Invoked if a value exists.</param>
    /// <param name="none">Invoked if no value exists.</param>
    public void Switch(Action<T> some, Action none) =>
        (IsSome ? some : _ => none())(_value!);

    /// <summary>
    /// Attempts to retrieve the contained value.
    /// </summary>
    /// <param name="value">The output value if present.</param>
    /// <returns><c>true</c> if a value is present; otherwise <c>false</c>.</returns>
    public bool TryGetValue(out T? value)
    {
        value = _value;
        return IsSome;
    }

    #endregion

    #region Object overrides & interface implementations

    public bool Equals(Option<T> other)
    {
        if (IsSome != other.IsSome)
        {
            return false;
        }

        if (IsNone)
        {
            return true;
        }

        return EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    public override string ToString() => IsSome ? $"Some({_value})" : "None";

    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);

    public override int GetHashCode() => IsSome ? EqualityComparer<T>.Default.GetHashCode(_value!) : 0;

    public IEnumerator<T> GetEnumerator()
    {
        if (IsSome)
        {
            yield return _value!;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Operator overloads

    public static bool operator ==(Option<T>? left, Option<T>? right) => EqualityComparer<Option<T>?>.Default.Equals(left, right);
    public static bool operator !=(Option<T>? left, Option<T>? right) => !(left == right);

    public static bool operator true(Option<T>? self) => self is not null && self.Value.IsSome;
    public static bool operator false(Option<T>? self) => self is null || self.Value.IsNone;

    public static implicit operator Option<T>(T? value) => From(value);
    public static implicit operator (bool, T?)(Option<T> self) => (self.IsSome, self._value);

    public static explicit operator T(Option<T> self) => self.Value;

    #endregion
}
