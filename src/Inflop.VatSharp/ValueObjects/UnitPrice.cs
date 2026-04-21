using Inflop.VatSharp.Enums;

namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// Represents the unit price of a product or service,
/// which can be either net or gross.
/// </summary>
public readonly record struct UnitPrice
{
    /// <summary>
    /// The monetary amount of the unit price.
    /// The interpretation of this amount (net or gross) is determined by the <see cref="Type"/> property.
    /// </summary>
    public Money Amount { get; }

    /// <summary>
    /// Indicates whether the unit price is net (excluding VAT) or gross (including VAT).
    /// </summary>
    public PriceType Type { get; }

    /// <summary>
    /// Factory method to create a net unit price from a decimal value.
    /// The provided value is wrapped in a <see cref="Money"/> struct and the price type is set to net.
    /// </summary>
    /// <param name="value">
    /// The decimal value representing the net unit price. Must be non-negative.
    /// </param>
    /// <returns>
    /// A <see cref="UnitPrice"/> instance representing the net unit price.
    /// </returns>
    public static UnitPrice Net(decimal value)
        => Net(Money.Of(value));

    /// <summary>
    /// Factory method to create a net unit price from a <see cref="Money"/> value.
    /// The price type is set to net.
    /// </summary>
    /// <param name="value">
    /// The money value representing the net unit price.
    /// </param>
    /// <returns>
    /// A <see cref="UnitPrice"/> instance representing the net unit price.
    /// </returns>
    public static UnitPrice Net(Money value)
        => new(value, PriceType.Net);

    /// <summary>
    /// Factory method to create a gross unit price from a decimal value.
    /// The provided value is wrapped in a <see cref="Money"/> struct and the price type is set to gross.
    /// </summary>
    /// <param name="value">
    /// The decimal value representing the gross unit price. Must be non-negative.
    /// </param>
    /// <returns>
    /// A <see cref="UnitPrice"/> instance representing the gross unit price.
    /// </returns>
    public static UnitPrice Gross(decimal value)
        => Gross(Money.Of(value));

    /// <summary>
    /// Factory method to create a gross unit price from a <see cref="Money"/> value.
    /// The price type is set to gross.
    /// </summary>
    /// <param name="value">
    /// The money value representing the gross unit price.
    /// </param>
    /// <returns>
    /// A <see cref="UnitPrice"/> instance representing the gross unit price.
    /// </returns>
    public static UnitPrice Gross(Money value)
        => new(value, PriceType.Gross);

    /// <summary>
    /// Returns true if the unit price is a net price (excluding VAT), false if it is a gross price (including VAT).
    /// This property is determined by the <see cref="Type"/> of the unit price.
    /// </summary>
    public bool IsNet
        => Type == PriceType.Net;

    /// <summary>
    /// Returns true if the unit price is a gross price (including VAT), false if it is a net price (excluding VAT).
    /// This property is determined by the <see cref="Type"/> of the unit price.
    /// </summary>
    public bool IsGross
        => Type == PriceType.Gross;

    /// <summary>
    /// Returns a string representation of the unit price, including the amount and the price type (net or gross).
    /// The format is "{Amount} ({Type})", e.g. "100.00 (Net)" or "120.00 (Gross)".
    /// Note that the amount is formatted using the <see cref="Money.ToString"/> method, which formats it with two decimal places.
    /// The price type is determined by the <see cref="Type"/> property and will be displayed as either "Net" or "Gross".
    /// This string representation is useful for debugging and logging purposes, but may not be suitable for end-user display without further formatting.
    /// </summary>
    public override string ToString()
        => $"{Amount} ({Type})";

    private UnitPrice(Money amount, PriceType type)
    {
        Amount = amount;
        Type = type;
    }
}