using Inflop.VatSharp.Exceptions;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Mapping;

/// <summary>
/// Compiled, immutable, thread-safe line item mapping.
/// </summary>
internal sealed class LineItemMapping<TLine>
{
    private readonly Func<TLine, UnitPrice> _price;
    private readonly Func<TLine, Quantity> _qty;
    private readonly Func<TLine, VatRate> _rate;
    private readonly Func<TLine, Discount?>? _discount;

    internal LineItemMapping(Func<TLine, UnitPrice> price, Func<TLine, Quantity> qty, Func<TLine, VatRate> rate, Func<TLine, Discount?>? discount = null)
    {
        _price = price ?? throw new ArgumentNullException(nameof(price));
        _qty = qty ?? throw new ArgumentNullException(nameof(qty));
        _rate = rate ?? throw new ArgumentNullException(nameof(rate));
        _discount = discount;
    }

    /// <summary>Maps a single source line item to an <see cref="InvoiceLineItem"/>.</summary>
    public InvoiceLineItem Map(TLine source)
    {
        ArgumentNullException.ThrowIfNull(source);
        try
        {
            return new (_price(source), _qty(source), _rate(source), _discount?.Invoke(source));
        }
        catch (Exception ex) when (ex is not MappingConfigurationException and not MappingExecutionException)
        {
            throw new MappingExecutionException($"Failed to map {typeof(TLine).Name}: {ex.Message}", ex);
        }
    }

    /// <summary>Maps all source line items to <see cref="InvoiceLineItem"/> instances.</summary>
    public IReadOnlyList<InvoiceLineItem> MapAll(IEnumerable<TLine> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        return sources.Select(Map).ToList();
    }
}