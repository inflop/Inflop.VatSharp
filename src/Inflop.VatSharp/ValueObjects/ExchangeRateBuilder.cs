namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// Fluent builder intermediate — first step. Do not construct directly; use <see cref="ExchangeRate.From"/>.
/// </summary>
public readonly record struct ExchangeRateFromStep
{
    /// <summary>The foreign (invoice) currency to convert from.</summary>
    public CurrencyCode ForeignCurrency { get; }

    /// <summary>
    /// Sets the base (settlement) currency and advances to the next builder step.
    /// </summary>
    /// <param name="baseCurrency">
    /// The settlement currency. Must differ from <see cref="ForeignCurrency"/>.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="baseCurrency"/> equals <see cref="ForeignCurrency"/>.</exception>
    public ExchangeRateToStep To(CurrencyCode baseCurrency)
    {
        if (ForeignCurrency == baseCurrency)
            throw new ArgumentException($"Foreign currency and base currency must differ, but both are '{ForeignCurrency}'.", nameof(baseCurrency));

        return new ExchangeRateToStep(ForeignCurrency, baseCurrency);
    }

    internal ExchangeRateFromStep(CurrencyCode foreignCurrency)
        => ForeignCurrency = foreignCurrency;
}

/// <summary>
/// Fluent builder intermediate — second step. Do not construct directly; use <see cref="ExchangeRateFromStep.To"/>.
/// </summary>
public readonly record struct ExchangeRateToStep
{
    /// <summary>The foreign (invoice) currency.</summary>
    public CurrencyCode ForeignCurrency { get; }

    /// <summary>The base (settlement) currency.</summary>
    public CurrencyCode BaseCurrency { get; }

    /// <summary>
    /// Sets the exchange rate value and advances to the final builder step.
    /// </summary>
    /// <param name="rate">Units of <see cref="BaseCurrency"/> per one unit of <see cref="ForeignCurrency"/>. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rate"/> is zero or negative.</exception>
    public ExchangeRateSpec Rate(decimal rate)
    {
        if (rate <= 0m)
            throw new ArgumentOutOfRangeException(nameof(rate), $"Exchange rate must be positive: {rate}.");

        return new ExchangeRateSpec(ForeignCurrency, BaseCurrency, rate);
    }

    internal ExchangeRateToStep(CurrencyCode foreignCurrency, CurrencyCode baseCurrency)
    {
        ForeignCurrency = foreignCurrency;
        BaseCurrency = baseCurrency;
    }
}

/// <summary>
/// Fluent builder intermediate — final step with implicit conversion to <see cref="ExchangeRate"/>.
/// Do not construct directly; use <see cref="ExchangeRateToStep.Rate"/>.
/// </summary>
public readonly record struct ExchangeRateSpec
{
    /// <summary>The foreign (invoice) currency.</summary>
    public CurrencyCode ForeignCurrency { get; }

    /// <summary>The base (settlement) currency.</summary>
    public CurrencyCode BaseCurrency { get; }

    /// <summary>Units of <see cref="BaseCurrency"/> per one unit of <see cref="ForeignCurrency"/>.</summary>
    public decimal RateValue { get; }

    /// <summary>Optional publication / effective date of the exchange rate.</summary>
    public DateOnly? RateDate { get; init; }

    /// <summary>Optional source institution label (e.g. "NBP", "ECB", "Bundesbank").</summary>
    public string? RateSource { get; init; }

    /// <summary>
    /// Sets the publication / effective date of the exchange rate.
    /// </summary>
    public ExchangeRateSpec Date(DateOnly date)
        => this with { RateDate = date };

    /// <summary>
    /// Sets the source institution label (e.g. "NBP", "ECB", "Bundesbank").
    /// </summary>
    public ExchangeRateSpec Source(string source)
        => this with { RateSource = source };

    /// <summary>
    /// Implicitly converts the builder to a fully validated <see cref="ExchangeRate"/>.
    /// </summary>
    public static implicit operator ExchangeRate(ExchangeRateSpec step)
        => ExchangeRate.Of(step.ForeignCurrency, step.BaseCurrency, step.RateValue, step.RateDate, step.RateSource);

    internal ExchangeRateSpec(CurrencyCode foreignCurrency, CurrencyCode baseCurrency, decimal rateValue)
    {
        ForeignCurrency = foreignCurrency;
        BaseCurrency = baseCurrency;
        RateValue = rateValue;
    }
}
