using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.Strategies.Rounding;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Strategies.Calculation;

/// <summary>
/// Strategy for a specific VAT calculation method.
/// </summary>
internal interface IVatCalculationStrategy
{
    /// <summary>
    /// The calculation method this strategy implements.
    /// Informational — used for self-documentation and potential future cross-validation.
    /// </summary>
    VatCalculationMethod Method { get; }

    DocumentAmounts Calculate(IReadOnlyList<InvoiceLineItem> lineItems, IRoundingStrategy rounding, IAbsoluteDiscountBehavior discountBehavior);

    DocumentAmounts Calculate(IReadOnlyList<InvoiceLineItem> lineItems, IRoundingStrategy rounding)
        => Calculate(lineItems, rounding, FromTotalAbsoluteDiscountBehavior.Instance);

    /// <summary>
    /// Converts a per-rate FCY summary to base currency using this strategy's authoritative field.
    /// Each strategy knows which field drives the others, so the conversion mirrors that hierarchy.
    /// </summary>
    VatRateSummaryFcy BuildSummaryFcy(VatRateSummary summary, ExchangeRate exchangeRate, IRoundingStrategy baseCurrencyRounding);
}
