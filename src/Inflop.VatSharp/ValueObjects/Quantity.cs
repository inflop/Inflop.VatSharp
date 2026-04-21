namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// Positive quantity value object. Ensures the quantity is always greater than zero.
/// </summary>
/// <remarks>
/// Intentionally implemented as <c>sealed record</c> (reference type) rather than
/// <c>readonly record struct</c>. A struct would expose an implicit parameterless
/// constructor, allowing <c>default(Quantity)</c> to produce an instance with
/// <c>Value = 0</c> — bypassing the positive-value invariant enforced in the
/// private constructor.
/// </remarks>
public sealed record Quantity : IComparable<Quantity>
{
    /// <summary>The underlying decimal quantity value.</summary>
    public decimal Value { get; }

    /// <summary>
    /// Creates a <see cref="Quantity"/> from a decimal value. Must be positive.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is zero or negative.</exception>
    public static Quantity Of(decimal value) => new(value);

    /// <summary>
    /// Creates a <see cref="Quantity"/> from an integer value. Must be positive.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is zero or negative.</exception>
    public static Quantity Of(int value) => new(value);

    /// <summary>A quantity of exactly one.</summary>
    public static Quantity One => new(1m);

    /// <inheritdoc />
    public int CompareTo(Quantity? other)
        => other is null ? 1 : Value.CompareTo(other.Value);

    /// <inheritdoc />
    public override string ToString()
        => Value.ToString("G");

    private Quantity(decimal value)
        => Value = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), $"Quantity must be positive: {value}.");
}
