using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.Strategies.Rounding;

namespace Inflop.VatSharp.Mapping;

/// <summary>
/// Registration pattern for DI containers.
/// No hard dependency on Microsoft.Extensions.DependencyInjection —
/// works with any container that supports factory registration.
///
/// <example>
/// <code>
/// // Direct factory — suitable for manual or framework DI registration:
/// services.AddSingleton(VatCalculationEngine.For&lt;Invoice, InvoiceLine&gt;(cfg => cfg
///     .Document(d => d.LineItems(f => f.Lines).Method(VatCalculationMethod.FromSumOfNetValues))
///     .LineItem(l => l.NetUnitPrice(p => p.UnitPrice).Quantity(p => p.Quantity).VatRate(p => p.VatRate))));
///
/// // Via the static helper (useful when the engine is configured separately from registration):
/// services.AddSingleton(VatCalculationRegistration.CreateEngine&lt;Invoice, InvoiceLine&gt;(cfg => cfg
///     .Document(d => d.LineItems(f => f.Lines).Method(VatCalculationMethod.FromSumOfNetValues))
///     .LineItem(l => l.NetUnitPrice(p => p.UnitPrice).Quantity(p => p.Quantity).VatRate(p => p.VatRate))));
/// </code>
/// </example>
/// </summary>
public static class VatCalculationRegistration
{
    /// <summary>
    /// Creates a configured, immutable engine instance suitable for singleton registration.
    /// The engine is thread-safe and can be shared across requests.
    /// </summary>
    public static VatCalculationEngine<TDoc, TLine> CreateEngine<TDoc, TLine>(Action<VatCalculationEngineBuilder<TDoc, TLine>> configure)
        => VatCalculationEngine.For(configure);

    /// <summary>
    /// Creates a configured line-item-only engine suitable for singleton registration.
    /// </summary>
    public static LineItemCalculationEngine<TLine> CreateItemEngine<TLine>(Action<LineItemMappingBuilder<TLine>> configure, IRoundingStrategy? rounding = null, IRoundingStrategy? baseCurrencyRounding = null, IAbsoluteDiscountBehavior? discountBehavior = null)
        => VatCalculationEngine.ForItems(configure, rounding, baseCurrencyRounding, discountBehavior);
}
