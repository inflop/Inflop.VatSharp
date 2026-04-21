namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// VAT summary row per rate — corresponds to the per-rate breakdown
/// required by art. 226 points 8–10 of Directive 2006/112/EC.
///
/// <see cref="TotalDiscount"/> captures the aggregate net-equivalent discount
/// for lines at this rate, as required by art. 226 pt 7 of Directive 2006/112/EC.
/// Zero when no discounts were applied.
/// </summary>
public sealed record VatRateSummary
{
    /// <summary>
    /// The VAT rate this summary row represents.
    /// </summary>
    public VatRate VatRate { get; }

    /// <summary>
    /// Sum of net values for all line items at this <see cref="VatRate"/>.
    /// </summary>
    public Money TotalNet { get; }

    /// <summary>
    /// Sum of VAT amounts for all line items at this <see cref="VatRate"/>.
    /// </summary>
    public Money TotalVat { get; }

    /// <summary>
    /// Sum of gross values for all line items at this <see cref="VatRate"/>.
    /// </summary>
    public Money TotalGross { get; }

    /// <summary>
    /// Sum of net-equivalent discounts for all line items at this <see cref="VatRate"/>.
    /// </summary>
    public Money TotalDiscount { get; }

    internal VatRateSummary(VatRate vatRate, Money totalNet, Money totalVat, Money totalGross, Money totalDiscount)
    {
        VatRate = vatRate;
        TotalNet = totalNet;
        TotalVat = totalVat;
        TotalGross = totalGross;
        TotalDiscount = totalDiscount;
    }
}
