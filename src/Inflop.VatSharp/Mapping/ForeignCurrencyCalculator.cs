using Inflop.VatSharp.ValueObjects;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Strategies.Calculation;
using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.Strategies.Rounding;

namespace Inflop.VatSharp.Mapping;

/// <summary>
/// Calculates VAT for foreign-currency invoices, producing amounts in both the
/// invoice currency and the base-currency VAT required for declarations.
///
/// <para>
/// The base (settlement) currency is jurisdiction-specific — configure via
/// <see cref="ExchangeRate.Of"/> or the fluent builder.
/// </para>
/// <para>
/// This calculator is a thin orchestration layer that:
/// <list type="number">
///   <item>Delegates arithmetic to the same <see cref="IVatCalculationStrategy"/> instances
///   used for domestic invoices — no calculation logic is duplicated.</item>
///   <item>Applies the supplied <see cref="ExchangeRate"/> to convert each per-rate VAT
///   summary to the base currency, rounding via the configured base rounding strategy.</item>
///   <item>Returns a <see cref="ForeignCurrencyDocumentAmounts"/> carrying both sets of figures.</item>
/// </list>
/// </para>
/// </summary>
internal sealed class ForeignCurrencyCalculator
{
    private readonly IRoundingStrategy _rounding;
    private readonly IRoundingStrategy _baseCurrencyRounding;
    private readonly IAbsoluteDiscountBehavior _discountBehavior;

    /// <param name="rounding">
    /// Rounding strategy for foreign-currency amounts. Defaults to 2 decimal places.
    /// </param>
    /// <param name="baseCurrencyRounding">
    /// Rounding strategy for the base-currency VAT conversion. Defaults to
    /// <see cref="DefaultRounding.TwoDecimalPlaces"/> (suitable for PLN, EUR, GBP, USD, CHF).
    /// Override for currencies with 0 or 3 decimal places (e.g. JPY, KWD).
    /// </param>
    /// <param name="discountBehavior">
    /// Discount application behavior. Defaults to <see cref="FromTotalAbsoluteDiscountBehavior.Instance"/>.
    /// </param>
    internal ForeignCurrencyCalculator(IRoundingStrategy? rounding = null, IRoundingStrategy? baseCurrencyRounding = null, IAbsoluteDiscountBehavior? discountBehavior = null)
    {
        _rounding = rounding ?? DefaultRounding.TwoDecimalPlaces;
        _baseCurrencyRounding = baseCurrencyRounding ?? DefaultRounding.TwoDecimalPlaces;
        _discountBehavior = discountBehavior ?? FromTotalAbsoluteDiscountBehavior.Instance;
    }

    /// <summary>
    /// Calculates VAT for a list of foreign-currency line items.
    /// </summary>
    /// <param name="lineItems">Line items with prices in the foreign currency.</param>
    /// <param name="method">Calculation method (art. 226 of Directive 2006/112/EC).</param>
    /// <param name="exchangeRate">
    /// The rate used to convert VAT to the base currency.
    /// The invoice currency is taken from <see cref="ExchangeRate.ForeignCurrency"/>.
    /// </param>
    public ForeignCurrencyDocumentAmounts Calculate(IReadOnlyList<InvoiceLineItem> lineItems, VatCalculationMethod method, ExchangeRate exchangeRate)
    {
        // Step 1: calculate in foreign currency using the existing domestic strategy
        var strategy = VatCalculationStrategyFactory.For(method);
        var domestic = strategy.Calculate(lineItems, _rounding, _discountBehavior);

        // Step 2: convert each per-rate summary to the base currency via the strategy itself
        var summariesFcy = BuildSummariesFcy(strategy, domestic.VatRateSummaries, exchangeRate);

        return new ForeignCurrencyDocumentAmounts(exchangeRate.ForeignCurrency, exchangeRate, method, summariesFcy, domestic.LineItems);
    }

    private IReadOnlyList<VatRateSummaryFcy> BuildSummariesFcy(IVatCalculationStrategy strategy, IReadOnlyList<VatRateSummary> summaries, ExchangeRate exchangeRate)
        => summaries.Select(s => strategy.BuildSummaryFcy(s, exchangeRate, _baseCurrencyRounding)).ToList();

}
