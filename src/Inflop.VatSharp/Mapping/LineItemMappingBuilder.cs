using Inflop.VatSharp.Exceptions;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Mapping;

/// <summary>
/// Fluent builder for configuring line item property mapping.
///
/// The discount configuration methods (<see cref="DiscountAbsolute(Func{TLine, decimal})"/>,
/// <see cref="DiscountPercentage(Func{TLine, decimal})"/>, <see cref="Discount(Func{TLine, Discount?})"/>)
/// are optional. When not configured, all line items are treated as having no discount.
/// </summary>
public sealed class LineItemMappingBuilder<TLine>
{
    internal Func<TLine, UnitPrice>? PriceFn { get; private set; }
    internal Func<TLine, Quantity>? QuantityFn { get; private set; }
    internal Func<TLine, VatRate>? VatRateFn { get; private set; }
    internal Func<TLine, Discount?>? DiscountFn { get; private set; }

    /// <summary>Maps a decimal field to a net unit price.</summary>
    public LineItemMappingBuilder<TLine> NetUnitPrice(Func<TLine, decimal> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        PriceFn = x => ValueObjects.UnitPrice.Net(fn(x));
        return this;
    }

    /// <summary>Maps a decimal field to a gross unit price.</summary>
    public LineItemMappingBuilder<TLine> GrossUnitPrice(Func<TLine, decimal> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        PriceFn = x => ValueObjects.UnitPrice.Gross(fn(x));
        return this;
    }

    /// <summary>Maps a <see cref="ValueObjects.UnitPrice"/> field directly.</summary>
    public LineItemMappingBuilder<TLine> UnitPrice(Func<TLine, UnitPrice> fn)
    {
        PriceFn = fn ?? throw new ArgumentNullException(nameof(fn));
        return this;
    }

    /// <summary>Maps a decimal field to <see cref="ValueObjects.Quantity"/>.</summary>
    public LineItemMappingBuilder<TLine> Quantity(Func<TLine, decimal> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        QuantityFn = x => ValueObjects.Quantity.Of(fn(x));
        return this;
    }

    /// <summary>Maps an integer field to <see cref="ValueObjects.Quantity"/>.</summary>
    public LineItemMappingBuilder<TLine> Quantity(Func<TLine, int> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        QuantityFn = x => ValueObjects.Quantity.Of(fn(x));
        return this;
    }

    /// <summary>Maps a <see cref="ValueObjects.Quantity"/> field directly.</summary>
    public LineItemMappingBuilder<TLine> Quantity(Func<TLine, Quantity> fn)
    {
        QuantityFn = fn ?? throw new ArgumentNullException(nameof(fn));
        return this;
    }

    /// <summary>Maps a decimal field to <see cref="ValueObjects.VatRate"/>.</summary>
    public LineItemMappingBuilder<TLine> VatRate(Func<TLine, decimal> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        VatRateFn = x => ValueObjects.VatRate.Of(fn(x));
        return this;
    }

    /// <summary>Maps an integer field to <see cref="ValueObjects.VatRate"/>.</summary>
    public LineItemMappingBuilder<TLine> VatRate(Func<TLine, int> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        VatRateFn = x => ValueObjects.VatRate.Of(fn(x));
        return this;
    }

    /// <summary>Maps a <see cref="ValueObjects.VatRate"/> field directly.</summary>
    public LineItemMappingBuilder<TLine> VatRate(Func<TLine, VatRate> fn)
    {
        VatRateFn = fn ?? throw new ArgumentNullException(nameof(fn));
        return this;
    }

    /// <summary>
    /// Maps a fixed monetary discount (absolute amount) from the source line item.
    /// The value is always applied to the line total, not per unit.
    /// </summary>
    public LineItemMappingBuilder<TLine> DiscountAbsolute(Func<TLine, decimal> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        DiscountFn = x => ValueObjects.Discount.OfAmount(fn(x));
        return this;
    }

    /// <summary>
    /// Maps an optional fixed monetary discount.
    /// Returns null (no discount) when the source value is null.
    /// </summary>
    public LineItemMappingBuilder<TLine> DiscountAbsolute(Func<TLine, decimal?> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        DiscountFn = x => fn(x) is { } v ? ValueObjects.Discount.OfAmount(v) : null;
        return this;
    }

    /// <summary>
    /// Maps a percentage discount (0–100) from the source line item.
    /// </summary>
    public LineItemMappingBuilder<TLine> DiscountPercentage(Func<TLine, decimal> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        DiscountFn = x => ValueObjects.Discount.OfPercentage(fn(x));
        return this;
    }

    /// <summary>
    /// Maps an optional percentage discount.
    /// Returns null (no discount) when the source value is null.
    /// </summary>
    public LineItemMappingBuilder<TLine> DiscountPercentage(Func<TLine, decimal?> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        DiscountFn = x => fn(x) is { } v ? ValueObjects.Discount.OfPercentage(v) : null;
        return this;
    }

    /// <summary>
    /// Maps a <see cref="Discount"/> value directly.
    /// Use when your source type already exposes a <see cref="Discount"/>
    /// or when you need dynamic dispatch between absolute and percentage.
    /// </summary>
    public LineItemMappingBuilder<TLine> Discount(Func<TLine, Discount?> fn)
    {
        DiscountFn = fn ?? throw new ArgumentNullException(nameof(fn));
        return this;
    }

    internal LineItemMapping<TLine> Build()
    {
        var name = typeof(TLine).Name;
        var priceFn = PriceFn ?? throw new MappingConfigurationException($"UnitPrice not configured for {name}.");
        var qtyFn = QuantityFn ?? throw new MappingConfigurationException($"Quantity not configured for {name}.");
        var rateFn = VatRateFn ?? throw new MappingConfigurationException($"VatRate not configured for {name}.");

        // DiscountFn is optional — null means "no discount on any line".
        return new LineItemMapping<TLine>(priceFn, qtyFn, rateFn, DiscountFn);
    }
}