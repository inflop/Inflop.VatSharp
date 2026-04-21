using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Exceptions;

namespace Inflop.VatSharp.Mapping;

/// <summary>
/// Compiled, immutable document mapping.
/// </summary>
internal sealed class DocumentMapping<TDoc, TLine>
{
    private readonly Func<TDoc, IEnumerable<TLine>> _lineItems;
    private readonly Func<TDoc, VatCalculationMethod> _method;
    private readonly DocumentMappingFcy<TDoc>? _fcy;

    /// <summary>Extracts line items from the source document.</summary>
    public IEnumerable<TLine> GetLineItems(TDoc doc)
    {
        try
        {
            return _lineItems(doc);
        }
        catch (Exception ex) when (ex is not MappingConfigurationException and not MappingExecutionException)
        {
            throw new MappingExecutionException($"Failed to extract line items from {typeof(TDoc).Name}: {ex.Message}", ex);
        }
    }

    /// <summary>Resolves the VAT calculation method from the source document.</summary>
    public VatCalculationMethod GetMethod(TDoc doc)
    {
        try
        {
            return _method(doc);
        }
        catch (Exception ex) when (ex is not MappingConfigurationException and not MappingExecutionException)
        {
            throw new MappingExecutionException($"Failed to extract calculation method from {typeof(TDoc).Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// True when ForeignCurrency was configured — enables <see cref="VatCalculationEngine{TDoc, TLine}.CalculateFcy"/>.
    /// </summary>
    public bool HasForeignCurrencyMapping => _fcy is not null;

    /// <summary>
    /// Extracts the exchange rate from the source document.
    /// </summary>
    /// <exception cref="MappingConfigurationException">
    /// Thrown when <see cref="HasForeignCurrencyMapping"/> is <c>false</c>.
    /// </exception>
    public ValueObjects.ExchangeRate GetExchangeRate(TDoc doc)
    {
        if (_fcy is null)
            throw new MappingConfigurationException("ForeignCurrency not configured.");

        try
        {
            return _fcy.GetExchangeRate(doc);
        }
        catch (Exception ex) when (ex is not MappingConfigurationException and not MappingExecutionException)
        {
            throw new MappingExecutionException($"Failed to extract exchange rate from {typeof(TDoc).Name}: {ex.Message}", ex);
        }
    }

    internal DocumentMapping(Func<TDoc, IEnumerable<TLine>> lineItems, Func<TDoc, VatCalculationMethod> method, DocumentMappingFcy<TDoc>? fcy = null)
    {
        _lineItems = lineItems ?? throw new ArgumentNullException(nameof(lineItems));
        _method = method ?? throw new ArgumentNullException(nameof(method));
        _fcy = fcy;
    }
}