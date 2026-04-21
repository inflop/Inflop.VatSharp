namespace Inflop.VatSharp.Strategies.Rounding;

/// <summary>
/// Arithmetic rounding to a given number of decimal places.
/// Midpoint (0.5) rounds away from zero — as required by most EU Member States
/// (DE §14 UStG, NL, AT, FI, FR, and most EU member states).
/// </summary>
public sealed class DefaultRounding : IRoundingStrategy
{
    /// <summary>Standard 2 decimal places (EUR, PLN, GBP, etc.).</summary>
    public static readonly DefaultRounding TwoDecimalPlaces = new(2);

    /// <summary>Zero decimal places (HUF — no fractional currency unit).</summary>
    public static readonly DefaultRounding ZeroDecimalPlaces = new(0);

    /// <summary>
    /// The number of decimal places used for rounding.
    /// </summary>
    public int DecimalPlaces { get; }

    /// <summary>
    /// Initializes a new <see cref="DefaultRounding"/> with the specified number of decimal places.
    /// </summary>
    /// <param name="decimalPlaces">
    /// Number of decimal places (0–8). Defaults to 2 (suitable for EUR, PLN, GBP, etc.).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="decimalPlaces"/> is outside 0–8.</exception>
    public DefaultRounding(int decimalPlaces = 2)
    {
        if (decimalPlaces < 0 || decimalPlaces > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), $"Decimal places must be 0–8: {decimalPlaces}.");
        }

        DecimalPlaces = decimalPlaces;
    }

    /// <inheritdoc />
    public decimal Round(decimal value)
        => decimal.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);

    /// <inheritdoc />
    public override string ToString()
        => $"Arithmetic({DecimalPlaces}dp, AwayFromZero)";
}
