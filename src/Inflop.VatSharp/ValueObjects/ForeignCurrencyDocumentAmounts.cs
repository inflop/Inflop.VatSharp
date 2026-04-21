using Inflop.VatSharp.Enums;

namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// Calculation result for a foreign-currency invoice.
///
/// Monetary amounts are expressed in both the invoice (foreign) currency and the
/// base (settlement) currency. Most jurisdictions require VAT to be declared in the
/// settlement currency (e.g. PLN for Poland, EUR for euro-area countries).
/// </summary>
public sealed record ForeignCurrencyDocumentAmounts
{
    /// <summary>
    /// The foreign currency in which net and gross amounts are expressed.
    /// </summary>
    public CurrencyCode Currency { get; }

    /// <summary>
    /// The exchange rate used to convert VAT to the base currency.
    /// </summary>
    public ExchangeRate ExchangeRate { get; }

    /// <summary>
    /// The calculation method that produced these amounts.
    /// </summary>
    public VatCalculationMethod Method { get; }

    /// <summary>
    /// Total net value in the invoice (foreign) currency.
    /// </summary>
    public Money TotalNet { get; }

    /// <summary>
    /// Total VAT amount in the invoice (foreign) currency.
    /// </summary>
    public Money TotalVat { get; }

    /// <summary>
    /// Total gross value in the invoice (foreign) currency.
    /// </summary>
    public Money TotalGross { get; }

    /// <summary>
    /// Total net-equivalent discount in the invoice (foreign) currency.
    /// </summary>
    public Money TotalDiscount { get; }

    /// <summary>
    /// Total net value converted to the base (settlement) currency.
    /// </summary>
    public Money TotalNetBase { get; }

    /// <summary>
    /// Total VAT amount converted to the base (settlement) currency.
    /// This is the legally required value for VAT declarations.
    /// </summary>
    public Money TotalVatBase { get; }

    /// <summary>
    /// Total gross value converted to the base (settlement) currency.
    /// </summary>
    public Money TotalGrossBase { get; }

    /// <summary>
    /// Total net-equivalent discount converted to the base (settlement) currency.
    /// </summary>
    public Money TotalDiscountBase { get; }

    /// <summary>
    /// Per-rate VAT breakdown with amounts in both the invoice and base currencies.
    /// </summary>
    public IReadOnlyList<VatRateSummaryFcy> VatRateSummaries { get; }

    /// <summary>
    /// Calculated amounts for each input line item in the invoice (foreign) currency.
    /// </summary>
    public IReadOnlyList<LineItemAmounts> LineItems { get; }

    /// <summary>
    /// Creates a <see cref="ForeignCurrencyDocumentAmounts"/> from per-rate FCY summaries and per-line amounts.
    /// Document-level totals are aggregated from <paramref name="summaries"/>.
    /// </summary>
    /// <param name="currency">The invoice (foreign) currency.</param>
    /// <param name="exchangeRate">The exchange rate used for base-currency conversion.</param>
    /// <param name="method">The <see cref="VatCalculationMethod"/> that produced the result.</param>
    /// <param name="summaries">Per-rate VAT summaries in both currencies.</param>
    /// <param name="lineItems">Per-line calculated amounts in the invoice currency.</param>
    internal ForeignCurrencyDocumentAmounts(CurrencyCode currency, ExchangeRate exchangeRate, VatCalculationMethod method,
                                            IReadOnlyList<VatRateSummaryFcy> summaries, IReadOnlyList<LineItemAmounts> lineItems)
    {
        Currency = currency;
        ExchangeRate = exchangeRate;
        Method = method;
        VatRateSummaries = summaries;
        LineItems = lineItems;

        TotalNet = summaries.Aggregate(Money.Zero, (acc, s) => acc + s.TotalNet);
        TotalVat = summaries.Aggregate(Money.Zero, (acc, s) => acc + s.TotalVat);
        TotalGross = summaries.Aggregate(Money.Zero, (acc, s) => acc + s.TotalGross);
        TotalDiscount = summaries.Aggregate(Money.Zero, (acc, s) => acc + s.TotalDiscount);

        TotalNetBase = summaries.Aggregate(Money.Zero, (acc, s) => acc + s.TotalNetBase);
        TotalVatBase = summaries.Aggregate(Money.Zero, (acc, s) => acc + s.TotalVatBase);
        TotalGrossBase = summaries.Aggregate(Money.Zero, (acc, s) => acc + s.TotalGrossBase);
        TotalDiscountBase = summaries.Aggregate(Money.Zero, (acc, s) => acc + s.TotalDiscountBase);
    }

    /// <summary>
    /// Projects into a standard <see cref="DocumentAmounts"/> where VAT rate summaries and
    /// document totals are expressed in the base (settlement) currency. Useful for
    /// base-currency-only pipelines (e.g. periodic VAT declarations, EC Sales Lists, JPK-VAT).
    /// </summary>
    /// <remarks>
    /// <see cref="DocumentAmounts.LineItems"/> remain in the invoice (foreign) currency.
    /// Only the VAT rate summaries and document totals are converted to the base currency.
    /// This is intentional: Polish VAT law (art. 106e ust. 11 ustawy o VAT) and
    /// EU Directive 2006/112/EC art. 91 require VAT amounts to be declared in the
    /// settlement currency — there is no legal requirement to convert individual line items.
    /// </remarks>
    public DocumentAmounts ToBaseDocumentAmounts()
        => new(Method, VatRateSummaries.Select(s => s.ToBaseSummary()).ToList(), LineItems);
}
