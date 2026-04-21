using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Exceptions;

namespace Inflop.VatSharp.Mapping;

/// <summary>
/// Fluent builder for document-level mapping (line items accessor + method selector).
/// </summary>
public sealed class DocumentMappingBuilder<TDoc, TLine>
{
    internal Func<TDoc, IEnumerable<TLine>>? LineItemsFn { get; private set; }
    internal Func<TDoc, VatCalculationMethod>? MethodFn { get; private set; }
    internal DocumentMappingFcy<TDoc>? Fcy { get; private set; }

    /// <summary>
    /// Maps the accessor that extracts line items from the source document.
    /// </summary>
    public DocumentMappingBuilder<TDoc, TLine> LineItems(Func<TDoc, IEnumerable<TLine>> fn)
    {
        LineItemsFn = fn ?? throw new ArgumentNullException(nameof(fn));
        return this;
    }

    /// <summary>
    /// Sets a constant <see cref="VatCalculationMethod"/> for all documents.
    /// </summary>
    public DocumentMappingBuilder<TDoc, TLine> Method(VatCalculationMethod method)
    {
        MethodFn = _ => method;
        return this;
    }

    /// <summary>
    /// Maps a per-document accessor that resolves the <see cref="VatCalculationMethod"/>.
    /// </summary>
    public DocumentMappingBuilder<TDoc, TLine> Method(Func<TDoc, VatCalculationMethod> fn)
    {
        MethodFn = fn ?? throw new ArgumentNullException(nameof(fn));
        return this;
    }

    /// <summary>
    /// Configures a constant exchange rate for all documents processed by this engine.
    /// Useful when all documents in a batch share the same rate (e.g. a monthly
    /// batch settled at a single central-bank rate).
    /// The invoice currency is derived from <see cref="ValueObjects.ExchangeRate.ForeignCurrency"/>.
    /// </summary>
    public DocumentMappingBuilder<TDoc, TLine> ForeignCurrency(ValueObjects.ExchangeRate exchangeRate)
    {
        ArgumentNullException.ThrowIfNull(exchangeRate);
        Fcy = new DocumentMappingFcy<TDoc>(_ => exchangeRate);
        return this;
    }

    /// <summary>
    /// Configures foreign-currency mapping via a per-document accessor. The invoice currency
    /// is derived from <see cref="ValueObjects.ExchangeRate.ForeignCurrency"/> — there is no
    /// separate currency mapping, eliminating any possibility of currency/rate mismatch.
    /// </summary>
    public DocumentMappingBuilder<TDoc, TLine> ForeignCurrency(Func<TDoc, ValueObjects.ExchangeRate> exchangeRateFn)
    {
        ArgumentNullException.ThrowIfNull(exchangeRateFn);
        Fcy = new DocumentMappingFcy<TDoc>(exchangeRateFn);
        return this;
    }

    internal DocumentMapping<TDoc, TLine> Build()
    {
        var name = typeof(TDoc).Name;
        var lineItemsFn = LineItemsFn ?? throw new MappingConfigurationException($"LineItems not configured for {name}.");
        var methodFn = MethodFn ?? throw new MappingConfigurationException($"Method not configured for {name}.");
        return new DocumentMapping<TDoc, TLine>(lineItemsFn, methodFn, Fcy);
    }
}