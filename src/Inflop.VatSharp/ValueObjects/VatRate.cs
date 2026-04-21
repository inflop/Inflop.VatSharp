namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// VAT rate as a percentage (e.g. 23 for 23%).
///
/// EU Directive 2006/112/EC allows standard (≥15%), reduced (≥5%),
/// super-reduced (&lt;5%), zero (0%), and parking (≥12%) rates.
/// The library accepts any rate 0–100% without enforcing category rules.
/// </summary>
public readonly record struct VatRate : IComparable<VatRate>
{
    /// <summary>
    /// Percentage value (e.g. 23 for 23%).
    /// </summary>
    public decimal Percentage { get; }

    /// <summary>
    /// Factory method to create a VAT rate from a decimal percentage value.
    /// The provided percentage must be between 0 and 100 inclusive; otherwise, an <see cref="ArgumentOutOfRangeException"/> is thrown.
    /// The resulting <see cref="VatRate"/> instance will have the specified percentage value, which can be used for VAT calculations and comparisons.
    /// </summary>
    /// <param name="percentage">
    /// The decimal percentage value representing the VAT rate (e.g. 23 for 23%).
    /// Must be between 0 and 100 inclusive.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the provided percentage is not between 0 and 100 inclusive.
    /// </exception>
    /// <returns>
    /// A <see cref="VatRate"/> instance representing the specified VAT rate percentage.
    /// </returns>
    public static VatRate Of(decimal percentage)
        => percentage is >= 0 and <= 100
            ? new(percentage)
            : throw new ArgumentOutOfRangeException(nameof(percentage), $"VAT rate must be 0–100%: {percentage}.");

    /// <summary>
    /// Factory method to create a VAT rate from an integer percentage value.
    /// The provided percentage must be between 0 and 100 inclusive; otherwise, an <see cref="ArgumentOutOfRangeException"/> is thrown.
    /// The resulting <see cref="VatRate"/> instance will have the specified percentage value, which can be used for VAT calculations and comparisons.
    /// </summary>
    /// <param name="percentage">
    /// The integer percentage value representing the VAT rate (e.g. 23 for 23%).
    /// Must be between 0 and 100 inclusive.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the provided percentage is not between 0 and 100 inclusive.
    /// </exception>
    /// <returns>
    /// A <see cref="VatRate"/> instance representing the specified VAT rate percentage.
    /// </returns>
    public static VatRate Of(int percentage)
        => Of((decimal)percentage);

    /// <summary>
    /// Decimal multiplier for calculations (e.g. 0.23 for 23%).
    /// </summary>
    public decimal Multiplier
        => Percentage / 100m;

    /// <summary>
    /// Zero rate (0%) — taxable supply at zero percent; input VAT is fully deductible
    /// per art. 169(a) of Directive 2006/112/EC.
    /// Typical for intra-Community supplies (art. 138) and exports (art. 146).
    /// Not to be confused with exempt supplies (art. 132–136), which carry no right to deduct.
    /// </summary>
    public static readonly VatRate Zero = new(0m);

    /// <summary>
    /// Returns true when the VAT rate is exactly zero.
    /// Note that this is not the same as being close to zero — a very small non-zero rate will return false.
    /// Zero-rated supplies are taxable at 0%; input VAT is deductible (art. 169(a) Directive 2006/112/EC).
    /// </summary>
    public bool IsZero
        => Percentage == Zero.Percentage;

    /// <summary>
    /// Calculates the VAT amount from a net price using this VAT rate.
    /// </summary>
    /// <remarks>
    /// The formula used is: VAT = net × (rate / 100).
    /// The result is intentionally unrounded — the caller applies the <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </remarks>
    /// <param name="net">
    /// The net price (excluding VAT) from which to calculate the VAT amount.
    /// </param>
    /// <returns>
    /// The VAT amount calculated from the net price. This is the portion of the net price that corresponds to VAT.
    /// The result is intentionally unrounded — the caller applies the <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </returns>
    public Money VatFromNet(Money net)
        => Money.Raw(net.Value * Multiplier);

    /// <summary>
    /// Calculates the VAT amount from a gross price.
    /// </summary>
    /// <remarks>
    /// The formula used is: VAT = gross × (rate / (100 + rate)).
    /// This formula derives from the relationship gross = net + VAT, where net = gross - VAT.
    /// The result is intentionally unrounded — the caller applies the <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </remarks>
    /// <param name="gross">
    /// The gross price (including VAT) from which to calculate the VAT amount.
    /// </param>
    /// <returns>
    /// The VAT amount calculated from the gross price. This is the portion of the gross price that corresponds to VAT.
    /// The result is intentionally unrounded — the caller applies the <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </returns>
    public Money VatFromGross(Money gross)
    {
        if (IsZero)
            return Money.Zero;

        return Money.Raw(gross.Value * Percentage / (100m + Percentage));
    }

    /// <summary>
    /// Calculates the gross price from a net price using this VAT rate.
    /// </summary>
    /// <remarks>
    /// The formula used is: gross = net + VAT(net) = net × (1 + rate / 100).
    /// The result is intentionally unrounded — the caller applies the <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </remarks>
    /// <param name="net">
    /// The net price (excluding VAT) from which to calculate the gross price.
    /// </param>
    /// <returns>
    /// The gross price calculated from the net price. This is the total price including VAT.
    /// The result is intentionally unrounded — the caller applies the <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </returns>
    public Money GrossFromNet(Money net)
        => net + VatFromNet(net);

    /// <summary>
    /// Calculates the net price from a gross price using this VAT rate.
    /// </summary>
    /// <remarks>
    /// The formula used is: net = gross - VAT(gross) = gross / (1 + rate / 100).
    /// </remarks>
    /// <param name="gross">
    /// The gross price (including VAT) from which to calculate the net price.
    /// </param>
    /// <returns>
    /// The net price calculated from the gross price. This is the price excluding VAT.
    /// The result is intentionally unrounded — the caller applies the <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </returns>
    public Money NetFromGross(Money gross)
        => Money.Raw(gross.Value - VatFromGross(gross).Value);

    /// <inheritdoc />
    public int CompareTo(VatRate other)
        => Percentage.CompareTo(other.Percentage);

    /// <summary>
    /// Returns a string representation of the VAT rate as a percentage followed by the percent sign (e.g. "23%" or "5.5%").
    /// </summary>
    public override string ToString()
        => $"{Percentage}%";

    private VatRate(decimal percentage)
        => Percentage = percentage;
}
