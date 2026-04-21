using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Strategies.Calculation;
using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.Strategies.Rounding;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Mapping;

/// <summary>
/// Simplified engine operating directly on a collection of line items (no document wrapper).
/// Created via <see cref="VatCalculationEngine.ForItems{TLine}"/>.
/// </summary>
public sealed class LineItemCalculationEngine<TLine>
{
    private readonly IRoundingStrategy _rounding;
    private readonly IAbsoluteDiscountBehavior _discountBehavior;
    private readonly LineItemMapping<TLine> _lineMapping;
    private readonly ForeignCurrencyCalculator _fcyCalc;

    internal LineItemCalculationEngine(IRoundingStrategy rounding, IRoundingStrategy? baseCurrencyRounding, IAbsoluteDiscountBehavior discountBehavior, LineItemMapping<TLine> lineMapping)
    {
        _rounding = rounding;
        _discountBehavior = discountBehavior;
        _lineMapping = lineMapping;
        _fcyCalc = new ForeignCurrencyCalculator(rounding, baseCurrencyRounding: baseCurrencyRounding, discountBehavior: discountBehavior);
    }

    /// <summary>
    /// Calculates document amounts from a collection of line items.
    /// </summary>
    /// <param name="lineItems">Source line items to calculate.</param>
    /// <param name="method">The <see cref="VatCalculationMethod"/> to apply.</param>
    public DocumentAmounts Calculate(IEnumerable<TLine> lineItems, VatCalculationMethod method)
    {
        ArgumentNullException.ThrowIfNull(lineItems);

        var items = _lineMapping.MapAll(lineItems);
        return VatCalculationStrategyFactory.For(method).Calculate(items, _rounding, _discountBehavior);
    }

    /// <summary>
    /// Calculates amounts for a single line item (e.g. for live preview in UI).
    /// </summary>
    public LineItemAmounts CalculateLineItem(TLine lineItem)
    {
        ArgumentNullException.ThrowIfNull(lineItem);
        return _lineMapping.Map(lineItem).Calculate(_rounding, _discountBehavior);
    }

    /// <summary>
    /// Calculates foreign-currency line items, producing amounts in both the invoice
    /// currency and the base (settlement) currency.
    /// </summary>
    /// <param name="lineItems">Line items with prices in the foreign currency.</param>
    /// <param name="method">Calculation method (art. 226 of Directive 2006/112/EC).</param>
    /// <param name="exchangeRate">
    /// The rate used to convert VAT to the base currency.
    /// The invoice currency is taken from <see cref="ExchangeRate.ForeignCurrency"/>.
    /// </param>
    public ForeignCurrencyDocumentAmounts Calculate(IEnumerable<TLine> lineItems, VatCalculationMethod method, ExchangeRate exchangeRate)
    {
        ArgumentNullException.ThrowIfNull(lineItems);

        var items = _lineMapping.MapAll(lineItems);
        return _fcyCalc.Calculate(items, method, exchangeRate);
    }
}