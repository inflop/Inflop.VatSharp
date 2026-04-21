namespace Inflop.VatSharp.Mapping;

/// <summary>
/// Encapsulates the foreign-currency mapping for a document type.
/// The invoice currency is always derived from <see cref="ValueObjects.ExchangeRate.ForeignCurrency"/>
/// — no separate currency mapping is needed, making currency/rate mismatch structurally impossible.
/// </summary>
internal sealed record DocumentMappingFcy<TDoc>(Func<TDoc, ValueObjects.ExchangeRate> ExchangeRateFn)
{
    public ValueObjects.ExchangeRate GetExchangeRate(TDoc doc) => ExchangeRateFn(doc);
}
