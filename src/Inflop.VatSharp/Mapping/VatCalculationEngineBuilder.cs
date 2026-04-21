using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.Strategies.Rounding;

namespace Inflop.VatSharp.Mapping;

/// <summary>
/// Fluent builder for constructing a <see cref="VatCalculationEngine{TDoc, TLine}"/>.
/// Configure document mapping, line item mapping, rounding, and discount behavior.
/// </summary>
public sealed class VatCalculationEngineBuilder<TDoc, TLine>
{
    private IRoundingStrategy? _rounding;
    private IRoundingStrategy? _baseCurrencyRounding;
    private IAbsoluteDiscountBehavior? _discountBehavior;
    private readonly DocumentMappingBuilder<TDoc, TLine> _docBuilder = new();
    private readonly LineItemMappingBuilder<TLine> _lineBuilder = new();

    /// <summary>
    /// Optional. Defaults to <see cref="DefaultRounding.TwoDecimalPlaces"/>.
    /// </summary>
    public VatCalculationEngineBuilder<TDoc, TLine> Rounding(IRoundingStrategy rounding)
    {
        _rounding = rounding ?? throw new ArgumentNullException(nameof(rounding));
        return this;
    }

    /// <summary>
    /// Optional base-currency rounding for FCY calculations.
    /// Defaults to <see cref="DefaultRounding.TwoDecimalPlaces"/>.
    /// Override for currencies with non-standard precision (e.g. JPY — 0dp, KWD — 3dp).
    /// </summary>
    public VatCalculationEngineBuilder<TDoc, TLine> BaseCurrencyRounding(IRoundingStrategy rounding)
    {
        _baseCurrencyRounding = rounding ?? throw new ArgumentNullException(nameof(rounding));
        return this;
    }

    /// <summary>
    /// Optional. Defaults to <see cref="FromTotalAbsoluteDiscountBehavior.Instance"/>.
    /// Use <see cref="PerUnitAbsoluteDiscountBehavior.Instance"/> to match systems
    /// that distribute the discount per unit before rounding.
    /// </summary>
    public VatCalculationEngineBuilder<TDoc, TLine> AbsoluteDiscount(IAbsoluteDiscountBehavior behavior)
    {
        _discountBehavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
        return this;
    }

    /// <summary>
    /// Applies the absolute discount directly to the line total: (price × qty) − discount.
    /// </summary>
    public VatCalculationEngineBuilder<TDoc, TLine> AbsoluteDiscountFromTotal()
        => AbsoluteDiscount(FromTotalAbsoluteDiscountBehavior.Instance);

    /// <summary>
    /// Distributes the discount per unit before multiplication: (price − round(discount / qty)) × qty.
    /// </summary>
    public VatCalculationEngineBuilder<TDoc, TLine> AbsoluteDiscountPerUnit()
        => AbsoluteDiscount(PerUnitAbsoluteDiscountBehavior.Instance);

    /// <summary>
    /// Configures document-level mapping (line items accessor, method selector, optional FCY).
    /// </summary>
    public VatCalculationEngineBuilder<TDoc, TLine> Document(Action<DocumentMappingBuilder<TDoc, TLine>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_docBuilder);
        return this;
    }

    /// <summary>
    /// Configures line item property mapping (unit price, quantity, VAT rate, optional discount).
    /// </summary>
    public VatCalculationEngineBuilder<TDoc, TLine> LineItem(Action<LineItemMappingBuilder<TLine>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_lineBuilder);
        return this;
    }

    /// <summary>
    /// Builds an immutable, thread-safe <see cref="VatCalculationEngine{TDoc, TLine}"/>.
    /// </summary>
    /// <exception cref="Exceptions.MappingConfigurationException">
    /// Thrown when required mappings (line items, method, unit price, quantity, or VAT rate) are not configured.
    /// </exception>
    public VatCalculationEngine<TDoc, TLine> Build()
        => new(_rounding ?? DefaultRounding.TwoDecimalPlaces, _baseCurrencyRounding, _discountBehavior ?? FromTotalAbsoluteDiscountBehavior.Instance, _docBuilder.Build(), _lineBuilder.Build());
}
