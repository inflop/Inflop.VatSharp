using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Strategies.Calculation;
using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.Strategies.Rounding;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Mapping;

/// <summary>
/// Immutable, thread-safe calculation engine configured for specific
/// document and line item types. Created via <see cref="VatCalculationEngine.For{TDoc,TLine}"/>.
/// </summary>
public sealed class VatCalculationEngine<TDoc, TLine>
{
    private readonly IRoundingStrategy _rounding;
    private readonly IAbsoluteDiscountBehavior _discountBehavior;
    private readonly DocumentMapping<TDoc, TLine> _docMapping;
    private readonly LineItemMapping<TLine> _lineMapping;
    private readonly ForeignCurrencyCalculator _fcyCalc;

    internal VatCalculationEngine(IRoundingStrategy rounding, IRoundingStrategy? baseCurrencyRounding, IAbsoluteDiscountBehavior discountBehavior, DocumentMapping<TDoc, TLine> docMapping, LineItemMapping<TLine> lineMapping)
    {
        _rounding = rounding;
        _discountBehavior = discountBehavior;
        _docMapping = docMapping;
        _lineMapping = lineMapping;
        _fcyCalc = new ForeignCurrencyCalculator(rounding, baseCurrencyRounding: baseCurrencyRounding, discountBehavior: discountBehavior);
    }

    /// <summary>
    /// Calculate document amounts. Method and line items resolved from mapping.
    /// </summary>
    public DocumentAmounts Calculate(TDoc document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var method = _docMapping.GetMethod(document);
        var items = _lineMapping.MapAll(_docMapping.GetLineItems(document));
        return VatCalculationStrategyFactory.For(method).Calculate(items, _rounding, _discountBehavior);
    }

    /// <summary>
    /// Calculate with method override (e.g. for comparing results).
    /// </summary>
    public DocumentAmounts Calculate(TDoc document, VatCalculationMethod methodOverride)
    {
        ArgumentNullException.ThrowIfNull(document);

        var items = _lineMapping.MapAll(_docMapping.GetLineItems(document));
        return VatCalculationStrategyFactory.For(methodOverride).Calculate(items, _rounding, _discountBehavior);
    }

    /// <summary>
    /// Calculate a single line item (e.g. for live preview in UI).
    /// </summary>
    public LineItemAmounts CalculateLineItem(TLine lineItem)
    {
        ArgumentNullException.ThrowIfNull(lineItem);
        return _lineMapping.Map(lineItem).Calculate(_rounding, _discountBehavior);
    }

    /// <summary>
    /// Calculates a foreign-currency document using an exchange rate read from the document
    /// mapping. The invoice currency is derived from
    /// <see cref="ExchangeRate.ForeignCurrency"/> — currency/rate mismatch is structurally
    /// impossible. Requires <c>ForeignCurrency</c> to have been configured in the
    /// <c>Document(...)</c> builder call.
    /// </summary>
    /// <exception cref="Exceptions.MappingConfigurationException">
    /// Thrown if <c>ForeignCurrency</c> was not configured.
    /// </exception>
    public ForeignCurrencyDocumentAmounts CalculateFcy(TDoc document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!_docMapping.HasForeignCurrencyMapping)
            throw new Exceptions.MappingConfigurationException("CalculateFcy requires ForeignCurrency to be configured in Document(...).");

        var method = _docMapping.GetMethod(document);
        var exchangeRate = _docMapping.GetExchangeRate(document);
        var items = _lineMapping.MapAll(_docMapping.GetLineItems(document));
        return _fcyCalc.Calculate(items, method, exchangeRate);
    }

    /// <summary>
    /// Calculates a foreign-currency document with currency and exchange rate supplied
    /// by the caller. The calculation method is resolved from the document mapping.
    /// </summary>
    /// <param name="document">The source document.</param>
    /// <param name="exchangeRate">
    /// The rate used to convert VAT to the base currency.
    /// The invoice currency is taken from <see cref="ExchangeRate.ForeignCurrency"/>.
    /// </param>
    public ForeignCurrencyDocumentAmounts Calculate(TDoc document, ExchangeRate exchangeRate)
    {
        ArgumentNullException.ThrowIfNull(document);

        var method = _docMapping.GetMethod(document);
        var items = _lineMapping.MapAll(_docMapping.GetLineItems(document));
        return _fcyCalc.Calculate(items, method, exchangeRate);
    }

    /// <summary>
    /// Calculates a foreign-currency document with an explicit method override.
    /// </summary>
    public ForeignCurrencyDocumentAmounts Calculate(TDoc document, VatCalculationMethod methodOverride, ExchangeRate exchangeRate)
    {
        ArgumentNullException.ThrowIfNull(document);

        var items = _lineMapping.MapAll(_docMapping.GetLineItems(document));
        return _fcyCalc.Calculate(items, methodOverride, exchangeRate);
    }
}