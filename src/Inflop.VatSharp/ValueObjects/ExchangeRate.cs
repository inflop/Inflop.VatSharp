using Inflop.VatSharp.Strategies.Rounding;

namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// Exchange rate used to convert foreign-currency VAT amounts to a base (settlement) currency.
///
/// The base currency is determined by the jurisdiction — e.g. EUR in the euro area,
/// PLN in Poland, CHF in Switzerland. The library is currency-agnostic.
///
/// Use <see cref="Of"/> for direct construction, or the fluent builder via <see cref="From"/>.
/// </summary>
/// <remarks>
/// Intentionally implemented as <c>sealed record</c> (reference type) rather than
/// <c>readonly record struct</c>. A struct would expose an implicit parameterless
/// constructor, allowing <c>default(ExchangeRate)</c> to produce an instance with
/// an invalid state (zero rate, null currencies) — bypassing the validation enforced
/// in the private constructor.
/// </remarks>
public sealed record ExchangeRate
{
    /// <summary>
    /// The foreign (invoice) currency being converted to <see cref="BaseCurrency"/>.
    /// </summary>
    public CurrencyCode ForeignCurrency { get; }

    /// <summary>
    /// The base (settlement) currency to which VAT amounts are converted.
    /// Determined by the jurisdiction — e.g. EUR for euro-area countries, PLN for Poland.
    /// </summary>
    public CurrencyCode BaseCurrency { get; }

    /// <summary>
    /// How many units of <see cref="BaseCurrency"/> one unit of <see cref="ForeignCurrency"/> is worth.
    /// E.g. 4.2345 means 1 EUR = 4.2345 PLN.
    /// </summary>
    public decimal Rate { get; }

    /// <summary>
    /// Optional label identifying the institution or source of the rate (e.g. "NBP", "ECB", "Bundesbank").
    /// Purely informational — not used in calculations.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// The date the rate was published / effective.
    /// Per Directive 2006/112/EC art. 91, the rate applicable on the date the tax becomes chargeable.
    /// <c>null</c> when the date is unknown or not required for the use case.
    /// </summary>
    public DateOnly? RateDate { get; }

    // ── Factory methods ──────────────────────────────────────────────────

    /// <summary>
    /// Creates an exchange rate.
    /// </summary>
    /// <param name="foreignCurrency">The invoice (foreign) currency.</param>
    /// <param name="baseCurrency">
    /// The settlement (base) currency. Must differ from <paramref name="foreignCurrency"/>.
    /// </param>
    /// <param name="rate">Units of <paramref name="baseCurrency"/> per one unit of <paramref name="foreignCurrency"/>.</param>
    /// <param name="rateDate">Publication / effective date of the rate. <c>null</c> if not applicable.</param>
    /// <param name="source">
    /// Optional label for the institution providing the rate (e.g. "NBP", "ECB", "Bundesbank").
    /// <c>null</c> if not applicable.
    /// </param>
    public static ExchangeRate Of(CurrencyCode foreignCurrency, CurrencyCode baseCurrency, decimal rate, DateOnly? rateDate = null, string? source = null)
        => new(foreignCurrency, baseCurrency, rate, source, rateDate);

    /// <summary>
    /// Starts a fluent builder chain: <c>ExchangeRate.From(CurrencyCode.EUR).To(CurrencyCode.PLN).Rate(4.21m).Date(date).Source("NBP")</c>.
    /// </summary>
    public static ExchangeRateFromStep From(CurrencyCode foreignCurrency)
        => new(foreignCurrency);

    /// <summary>
    /// Converts a foreign-currency amount to <see cref="BaseCurrency"/> using this rate.
    /// The result is unrounded — caller applies the rounding strategy.
    /// </summary>
    public Money ConvertToBase(Money foreignAmount)
        => Money.Raw(foreignAmount.Value * Rate);

    /// <summary>
    /// Converts a foreign-currency amount to <see cref="BaseCurrency"/> and rounds immediately.
    /// </summary>
    public Money ConvertToBase(Money foreignAmount, IRoundingStrategy rounding)
        => ConvertToBase(foreignAmount).Round(rounding);

    /// <summary>
    /// Returns a human-readable representation of the rate, e.g. <c>1 EUR = 4.2140 PLN (NBP, 2026-02-25)</c>.
    /// </summary>
    public override string ToString()
    {
        var basePart = FormattableString.Invariant($"1 {ForeignCurrency} = {Rate:F4} {BaseCurrency}");
        var datePart = RateDate.HasValue ? FormattableString.Invariant($"{RateDate.Value:yyyy-MM-dd}") : null;
        return (Source, datePart) switch
        {
            (null, null) => basePart,
            (null, _) => $"{basePart} ({datePart})",
            (_, null) => $"{basePart} ({Source})",
            _ => $"{basePart} ({Source}, {datePart})"
        };
    }

    private ExchangeRate(CurrencyCode foreignCurrency, CurrencyCode baseCurrency, decimal rate, string? source, DateOnly? rateDate)
    {
        ArgumentNullException.ThrowIfNull(foreignCurrency);
        ArgumentNullException.ThrowIfNull(baseCurrency);

        if (foreignCurrency == baseCurrency)
            throw new ArgumentException($"Foreign currency and base currency must differ, but both are '{foreignCurrency}'. An ExchangeRate is only needed when the invoice currency differs from the settlement currency.", nameof(foreignCurrency));

        if (rate <= 0m)
            throw new ArgumentOutOfRangeException(nameof(rate), $"Exchange rate must be positive: {rate}.");

        ForeignCurrency = foreignCurrency;
        BaseCurrency = baseCurrency;
        Rate = rate;
        Source = source;
        RateDate = rateDate;
    }
}
