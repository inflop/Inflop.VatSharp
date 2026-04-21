using Inflop.VatSharp.Enums;

namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// Immutable price reduction applied to a line item before VAT calculation.
///
/// Legal basis: art. 79 lit. b of Directive 2006/112/EC — discounts and rebates
/// granted at the time of supply reduce the taxable amount.
///
/// The discount is applied to the line total (UnitPrice × Quantity) in the
/// same price type as the unit price (net for net prices, gross for gross).
/// Calculation strategies always work with the net-equivalent discount
/// exposed via <see cref="InvoiceLineItem.DiscountAmountNetWith"/>.
/// </summary>
public readonly record struct Discount
{
    // Stores the defining scalar: absolute amount OR percentage value.
    private readonly decimal _value;

    /// <summary>
    /// Discriminates between absolute and percentage discounts.
    /// </summary>
    public DiscountType Type { get; }

    /// <summary>
    /// Creates a fixed monetary discount off the pre-tax line total.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is negative.</exception>
    public static Discount OfAmount(decimal amount)
        => OfAmount(Money.Of(amount));

    /// <summary>
    /// Creates a fixed monetary discount off the pre-tax line total.
    /// </summary>
    public static Discount OfAmount(Money amount)
        => new(DiscountType.Absolute, amount.Value);

    /// <summary>
    /// Creates a percentage-based discount (0–100 %) off the pre-tax line total.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when percentage is outside 0–100.</exception>
    public static Discount OfPercentage(decimal percentage)
        => percentage is >= 0m and <= 100m ?
            new(DiscountType.Percentage, percentage) :
            throw new ArgumentOutOfRangeException(nameof(percentage), $"Discount percentage must be 0–100: {percentage}.");

    // ──────────────────────────── Accessors ────────────────────────────────

    /// <summary>
    /// The percentage value (0–100).
    /// Only valid when <see cref="Type"/> is <see cref="DiscountType.Percentage"/>.
    /// </summary>
    public decimal Percentage
        => Type == DiscountType.Percentage ?
            _value :
            throw new InvalidOperationException("This discount is absolute, not percentage-based.");

    /// <summary>
    /// The absolute monetary amount.
    /// Only valid when <see cref="Type"/> is <see cref="DiscountType.Absolute"/>.
    /// </summary>
    public Money AbsoluteAmount
        => Type == DiscountType.Absolute ?
            Money.Of(_value) :
            throw new InvalidOperationException("This discount is percentage-based, not absolute.");

    /// <summary>
    /// Returns true when the discount will produce a zero deduction.
    /// </summary>
    public bool IsZero
        => _value == 0m;

    /// <summary>
    /// Calculates the monetary discount from the supplied pre-discount total.
    /// The result is intentionally unrounded — the caller applies the
    /// <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </summary>
    /// <param name="baseAmount">Pre-discount line total (net × qty or gross × qty).</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an absolute discount exceeds the base amount (would yield negative net).
    /// </exception>
    public Money CalculateFrom(Money baseAmount)
        => Type switch
        {
            DiscountType.Absolute when _value > baseAmount.Value
                => throw new InvalidOperationException($"Absolute discount {_value:N2} exceeds base amount {baseAmount.Value:N2}. The effective price cannot be negative."),
            DiscountType.Absolute => Money.Of(_value),
            DiscountType.Percentage => Money.Raw(baseAmount.Value * _value / 100m),
            _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, null)
        };

    /// <inheritdoc/>
    public override string ToString()
        => Type switch
        {
            DiscountType.Percentage => $"-{_value:G}%",
            DiscountType.Absolute => $"-{_value:N2}",
            _ => "Discount"
        };

    private Discount(DiscountType type, decimal value)
    {
        Type = type;
        _value = value;
    }
}