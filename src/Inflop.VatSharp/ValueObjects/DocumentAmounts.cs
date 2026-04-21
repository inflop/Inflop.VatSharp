using Inflop.VatSharp.Enums;

namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// Complete document calculation result.
///
/// <see cref="TotalDiscount"/> is the document-level aggregate of all net-equivalent
/// line discounts (art. 79 lit. b of Directive 2006/112/EC). Zero when no discounts
/// were applied on any line.
/// </summary>
public sealed record DocumentAmounts
{
    /// <summary>
    /// The <see cref="VatCalculationMethod"/> used to produce these amounts.
    /// </summary>
    public VatCalculationMethod Method { get; }

    /// <summary>
    /// Total net value of all line items (sum of <see cref="VatRateSummary.TotalNet"/> per rate).
    /// </summary>
    public Money TotalNet { get; }

    /// <summary>
    /// Total VAT amount (sum of <see cref="VatRateSummary.TotalVat"/> per rate).
    /// </summary>
    public Money TotalVat { get; }

    /// <summary>
    /// Total gross value of all line items (sum of <see cref="VatRateSummary.TotalGross"/> per rate).
    /// </summary>
    public Money TotalGross { get; }

    /// <summary>
    /// Sum of net-equivalent discounts across all lines and all VAT rates.
    /// Represents the total price reduction shown on the document per
    /// art. 79 lit. b of Directive 2006/112/EC.
    /// </summary>
    public Money TotalDiscount { get; }

    /// <summary>
    /// Per-rate VAT breakdown required by art. 226 points 8–10 of Directive 2006/112/EC.
    /// </summary>
    public IReadOnlyList<VatRateSummary> VatRateSummaries { get; }

    /// <summary>
    /// Calculated amounts for each input line item, in the same order as the input.
    /// </summary>
    public IReadOnlyList<LineItemAmounts> LineItems { get; }

    /// <summary>
    /// Creates a <see cref="DocumentAmounts"/> from per-rate summaries and per-line amounts.
    /// Document-level totals are aggregated from <paramref name="summaries"/>.
    /// </summary>
    /// <param name="method">The <see cref="VatCalculationMethod"/> that produced the result.</param>
    /// <param name="summaries">Per-rate VAT summaries.</param>
    /// <param name="lineItems">Per-line calculated amounts.</param>
    internal DocumentAmounts(VatCalculationMethod method, IReadOnlyList<VatRateSummary> summaries, IReadOnlyList<LineItemAmounts> lineItems)
    {
        Method = method;
        VatRateSummaries = summaries;
        LineItems = lineItems;

        TotalNet = summaries.Aggregate(Money.Zero, (acc, r) => acc + r.TotalNet);
        TotalVat = summaries.Aggregate(Money.Zero, (acc, r) => acc + r.TotalVat);
        TotalGross = summaries.Aggregate(Money.Zero, (acc, r) => acc + r.TotalGross);
        TotalDiscount = summaries.Aggregate(Money.Zero, (acc, r) => acc + r.TotalDiscount);
    }
}