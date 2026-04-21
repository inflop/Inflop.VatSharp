using Inflop.VatSharp.Mapping;
using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.Strategies.Rounding;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp;

/// <summary>
/// Static factory — single entry point for all VAT calculation scenarios.
/// <list type="bullet">
///   <item><see cref="Create"/> — already have <see cref="InvoiceLineItem"/> objects</item>
///   <item><see cref="For{TDoc,TLine}"/> — have your own document + line item types</item>
///   <item><see cref="ForItems{TLine}"/> — have your own line item types, no document wrapper</item>
/// </list>
/// </summary>
public static class VatCalculationEngine
{
    /// <summary>
    /// Engine for direct use with the library's own <see cref="InvoiceLineItem"/> value objects.
    /// No mapping configuration required.
    /// </summary>
    public static LineItemCalculationEngine<InvoiceLineItem> Create(IRoundingStrategy? rounding = null, IRoundingStrategy? baseCurrencyRounding = null, IAbsoluteDiscountBehavior? discountBehavior = null)
        => ForItems<InvoiceLineItem>(cfg => cfg
            .UnitPrice(x => x.UnitPrice)
            .Quantity(x => x.Quantity)
            .VatRate(x => x.VatRate)
            .Discount(x => x.Discount),
            rounding,
            baseCurrencyRounding,
            discountBehavior);

    /// <summary>
    /// Full engine with document wrapper.
    /// Returns an immutable, thread-safe <see cref="VatCalculationEngine{TDoc, TLine}"/>.
    /// </summary>
    public static VatCalculationEngine<TDoc, TLine> For<TDoc, TLine>(Action<VatCalculationEngineBuilder<TDoc, TLine>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new VatCalculationEngineBuilder<TDoc, TLine>();
        configure(builder);
        return builder.Build();
    }

    /// <summary>
    /// Simplified engine operating directly on a collection of line items (no document wrapper).
    /// Returns a <see cref="LineItemCalculationEngine{TLine}"/>.
    /// </summary>
    public static LineItemCalculationEngine<TLine> ForItems<TLine>(Action<LineItemMappingBuilder<TLine>> configure, IRoundingStrategy? rounding = null, IRoundingStrategy? baseCurrencyRounding = null, IAbsoluteDiscountBehavior? discountBehavior = null)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new LineItemMappingBuilder<TLine>();
        configure(builder);

        rounding ??= DefaultRounding.TwoDecimalPlaces;
        discountBehavior ??= FromTotalAbsoluteDiscountBehavior.Instance;
        var lineMapping = builder.Build();

        return new LineItemCalculationEngine<TLine>(rounding, baseCurrencyRounding, discountBehavior, lineMapping);
    }
}
