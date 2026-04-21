using Inflop.VatSharp.Strategies.Rounding;

namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// Immutable monetary amount. Currency-agnostic — rounding is applied
/// externally by the calculation pipeline via <see cref="IRoundingStrategy"/>.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    /// <summary>
    /// The monetary value. Must be non-negative.
    /// Rounding is applied externally by the calculation pipeline via <see cref="IRoundingStrategy"/>.
    /// </summary>
    public decimal Value { get; }

    /// <summary>
    /// Creates a money amount from a decimal value.
    /// The value must be non-negative.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is negative.
    /// </exception>
    public static Money Of(decimal value)
        => value >= 0
            ? new(value)
            : throw new ArgumentOutOfRangeException(nameof(value), $"The money value cannot be negative: {value}.");

    /// <summary>
    /// A zero money amount.
    /// Useful as a default value or for representing zero discounts.
    /// </summary>
    public static readonly Money Zero = Of(0m);

    /// <summary>
    /// Creates a money amount from a decimal value.
    /// The value must be non-negative.
    /// </summary>
    /// <param name="rounding">
    /// The rounding strategy to apply to the result.
    /// </param>
    /// <returns>
    /// The money amount rounded according to the provided strategy.
    /// </returns>
    public Money Round(IRoundingStrategy rounding)
        => Of(rounding.Round(Value));

    /// <summary>
    /// Returns true when the amount is exactly zero.
    /// Note that this is not the same as being close to zero — a very small non-zero amount will return false.
    /// </summary>
    public bool IsZero
        => Value == Zero.Value;

    /// <summary>
    /// Returns the sum of two amounts.
    /// </summary>
    public static Money operator +(Money a, Money b)
        => Of(a.Value + b.Value);

    /// <summary>
    /// Returns the difference of two amounts. Throws when the result would be negative.
    /// </summary>
    public static Money operator -(Money a, Money b)
        => Of(a.Value - b.Value);

    /// <summary>
    /// Multiplies the amount by a non-negative decimal factor.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="factor"/> is negative.</exception>
    public static Money operator *(Money m, decimal factor)
        => factor >= 0m
            ? Raw(m.Value * factor)
            : throw new ArgumentOutOfRangeException(nameof(factor), $"Multiplication factor cannot be negative: {factor}.");

    /// <summary>
    /// Multiplies the unit price by a quantity.
    /// </summary>
    public static Money operator *(Money m, Quantity q)
        => Raw(m.Value * q.Value);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="a"/> is greater than <paramref name="b"/>.
    /// </summary>
    public static bool operator >(Money a, Money b)
        => a.Value > b.Value;

    /// <summary>
    /// Returns <c>true</c> when <paramref name="a"/> is less than <paramref name="b"/>.
    /// </summary>
    public static bool operator <(Money a, Money b)
        => a.Value < b.Value;

    /// <summary>
    /// Returns <c>true</c> when <paramref name="a"/> is greater than or equal to <paramref name="b"/>.
    /// </summary>
    public static bool operator >=(Money a, Money b)
        => a.Value >= b.Value;

    /// <summary>
    /// Returns <c>true</c> when <paramref name="a"/> is less than or equal to <paramref name="b"/>.
    /// </summary>
    public static bool operator <=(Money a, Money b)
        => a.Value <= b.Value;

    /// <inheritdoc />
    public int CompareTo(Money other)
        => Value.CompareTo(other.Value);

    /// <summary>
    /// Returns a string representation of the money amount, formatted with two decimal places.
    /// Note that this does not include any currency symbol or code, as Money is currency-agnostic.
    /// </summary>
    public override string ToString()
        => Value.ToString("N2");

    /// <summary>
    /// Creates a money amount from a decimal value without validation or rounding.
    /// </summary>
    /// <param name="value">
    /// The raw decimal value to wrap in a Money struct.
    /// No validation is performed, so it can be negative or have more than two decimal places.
    /// </param>
    /// <returns>
    /// A Money struct wrapping the provided decimal value.
    /// </returns>
    internal static Money Raw(decimal value)
        => new(value);

    private Money(decimal value)
        => Value = value;
}
