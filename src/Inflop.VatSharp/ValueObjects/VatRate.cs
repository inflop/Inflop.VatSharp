namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// VAT rate as a percentage (e.g. 23 for 23%) together with its invoice symbol (e.g. "23%", "ZW", "NP").
///
/// EU Directive 2006/112/EC allows standard (≥15%), reduced (≥5%),
/// super-reduced (&lt;5%), zero (0%), and parking (≥12%) rates.
/// The library accepts any rate 0–100% without enforcing category rules.
///
/// <para>
/// The <see cref="Symbol"/> property is part of value-object equality and serves as the grouping key
/// for <see cref="VatRateSummary"/> rows (art. 226 pts 8–10 of Directive 2006/112/EC).
/// This correctly separates legally distinct zero-rate categories — such as Polish "0%" (zero-rated,
/// art. 83 ustawy o VAT), "ZW" (exempt, art. 43 ustawy o VAT), and "NP" (not subject to VAT /
/// reverse charge) — into separate summary rows even though all share <see cref="Percentage"/> = 0.
/// </para>
///
/// <para>
/// Implemented as a <c>sealed record</c> (reference type) rather than a <c>readonly record struct</c>
/// to prevent <c>default(VatRate)</c> from producing a <c>null</c> <see cref="Symbol"/>, bypassing
/// the factory invariant. <c>default(VatRate)</c> yields <c>null</c>, caught at compile time by
/// nullable reference type analysis.
/// </para>
/// </summary>
public sealed record VatRate : IComparable<VatRate>
{
    /// <summary>
    /// Percentage value (e.g. 23 for 23%).
    /// </summary>
    public decimal Percentage { get; }

    /// <summary>
    /// Invoice symbol identifying the VAT category (e.g. "23%", "8%", "0%", "ZW", "NP").
    /// Used as the grouping key for VAT rate summaries alongside <see cref="Percentage"/>.
    /// Defaults to the percentage string (e.g. "23%") when not specified explicitly.
    /// Country-neutral: use national conventions ("ZW", "NP") or PEPPOL Tax Category Codes
    /// ("E", "AE", "O") as appropriate.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Factory method to create a VAT rate from a decimal percentage value.
    /// <see cref="Symbol"/> defaults to the invariant-culture percentage string (e.g. "23%", "5.5%").
    /// Use <see cref="Of(decimal, string)"/> to supply an explicit symbol such as "ZW" or "NP".
    /// </summary>
    /// <param name="percentage">
    /// The decimal percentage value representing the VAT rate (e.g. 23 for 23%).
    /// Must be between 0 and 100 inclusive.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the provided percentage is not between 0 and 100 inclusive.
    /// </exception>
    public static VatRate Of(decimal percentage)
        => Of(percentage, FormattableString.Invariant($"{percentage}%"));

    /// <summary>
    /// Factory method to create a VAT rate from a decimal percentage value and an explicit symbol.
    /// Use this overload to distinguish legally distinct zero-rate categories such as
    /// "0%" (zero-rated), "ZW" (exempt — art. 43 ustawy o VAT), or "NP" (not subject to VAT).
    /// Each unique symbol produces a separate <see cref="VatRateSummary"/> row per art. 226
    /// pts 8–10 of Directive 2006/112/EC.
    /// </summary>
    /// <param name="percentage">
    /// The decimal percentage value (e.g. 0 for zero / exempt / not-subject categories).
    /// Must be between 0 and 100 inclusive.
    /// </param>
    /// <param name="symbol">
    /// The invoice symbol for this rate category (e.g. "ZW", "NP", "0%", "E", "AE").
    /// Must not be null or whitespace.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="percentage"/> is not between 0 and 100 inclusive.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="symbol"/> is null or whitespace.
    /// </exception>
    public static VatRate Of(decimal percentage, string symbol)
    {
        if (percentage is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(percentage), $"VAT rate must be 0–100%: {percentage}.");
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        return new(percentage, symbol);
    }

    /// <summary>
    /// Factory method to create a VAT rate from an integer percentage value.
    /// <see cref="Symbol"/> defaults to the percentage string (e.g. "23%").
    /// </summary>
    /// <param name="percentage">
    /// The integer percentage value representing the VAT rate (e.g. 23 for 23%).
    /// Must be between 0 and 100 inclusive.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the provided percentage is not between 0 and 100 inclusive.
    /// </exception>
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
    /// Symbol is "0%". For Polish "ZW" or "NP" use <see cref="Of(decimal, string)"/>.
    /// </summary>
    public static readonly VatRate Zero = new(0m, "0%");

    /// <summary>
    /// Returns true when the VAT rate percentage is exactly zero, regardless of <see cref="Symbol"/>.
    /// Applies to all zero-percentage categories including "0%", "ZW", and "NP".
    /// Zero-rated supplies (0%) are taxable at 0%; input VAT is deductible (art. 169(a) Directive 2006/112/EC).
    /// </summary>
    public bool IsZero
        => Percentage == 0m;

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
    /// The VAT amount calculated from the net price.
    /// The result is intentionally unrounded — the caller applies the <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </returns>
    public Money VatFromNet(Money net)
        => Money.Raw(net.Value * Multiplier);

    /// <summary>
    /// Calculates the VAT amount from a gross price.
    /// </summary>
    /// <remarks>
    /// The formula used is: VAT = gross × (rate / (100 + rate)).
    /// The result is intentionally unrounded — the caller applies the <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </remarks>
    /// <param name="gross">
    /// The gross price (including VAT) from which to calculate the VAT amount.
    /// </param>
    /// <returns>
    /// The VAT amount calculated from the gross price.
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
    /// The gross price calculated from the net price.
    /// The result is intentionally unrounded — the caller applies the <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </returns>
    public Money GrossFromNet(Money net)
        => net + VatFromNet(net);

    /// <summary>
    /// Calculates the net price from a gross price using this VAT rate.
    /// </summary>
    /// <remarks>
    /// The formula used is: net = gross - VAT(gross) = gross / (1 + rate / 100).
    /// The result is intentionally unrounded — the caller applies the <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </remarks>
    /// <param name="gross">
    /// The gross price (including VAT) from which to calculate the net price.
    /// </param>
    /// <returns>
    /// The net price calculated from the gross price.
    /// The result is intentionally unrounded — the caller applies the <see cref="Strategies.Rounding.IRoundingStrategy"/>.
    /// </returns>
    public Money NetFromGross(Money gross)
        => Money.Raw(gross.Value - VatFromGross(gross).Value);

    /// <inheritdoc />
    public int CompareTo(VatRate? other)
    {
        if (other is null) return 1;
        var byPercentage = Percentage.CompareTo(other.Percentage);
        return byPercentage != 0 ? byPercentage : string.Compare(Symbol, other.Symbol, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns the <see cref="Symbol"/> (e.g. "23%", "ZW", "NP").
    /// </summary>
    public override string ToString()
        => Symbol;

    private VatRate(decimal percentage, string symbol)
    {
        Percentage = percentage;
        Symbol = symbol;
    }
}
