namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// VAT summary row per rate for a foreign-currency invoice.
///
/// <para>
/// <see cref="TotalNet"/>, <see cref="TotalVat"/>, <see cref="TotalGross"/>, and <see cref="TotalDiscount"/> are expressed
/// in the invoice's foreign currency. The <c>*Base</c> counterparts hold the same amounts
/// converted to the base (settlement) currency — mandated for VAT declarations by most
/// jurisdictions (e.g. PLN for Poland, EUR for euro-area countries).
/// </para>
/// </summary>
public sealed record VatRateSummaryFcy
{
    /// <summary>
    /// The VAT rate this summary row represents.
    /// </summary>
    public VatRate VatRate { get; }

    /// <summary>
    /// Net total in the invoice (foreign) currency.
    /// </summary>
    public Money TotalNet { get; }

    /// <summary>
    /// VAT total in the invoice (foreign) currency.
    /// </summary>
    public Money TotalVat { get; }

    /// <summary>
    /// Gross total in the invoice (foreign) currency.
    /// </summary>
    public Money TotalGross { get; }

    /// <summary>
    /// Net-equivalent discount in the invoice (foreign) currency.
    /// </summary>
    public Money TotalDiscount { get; }

    /// <summary>
    /// Net total converted to the base (settlement) currency.
    /// </summary>
    public Money TotalNetBase { get; }

    /// <summary>
    /// VAT total converted to the base (settlement) currency.
    /// </summary>
    public Money TotalVatBase { get; }

    /// <summary>
    /// Gross total converted to the base (settlement) currency.
    /// </summary>
    public Money TotalGrossBase { get; }

    /// <summary>
    /// Net-equivalent discount converted to the base (settlement) currency.
    /// </summary>
    public Money TotalDiscountBase { get; }

    /// <summary>
    /// Projects into a domestic <see cref="VatRateSummary"/> denominated entirely in the base currency.
    /// </summary>
    public VatRateSummary ToBaseSummary()
        => new(VatRate, TotalNetBase, TotalVatBase, TotalGrossBase, TotalDiscountBase);

    internal VatRateSummaryFcy(VatRate vatRate, Money totalNet, Money totalVat, Money totalGross, Money totalDiscount,
                                Money totalNetBase, Money totalVatBase, Money totalGrossBase, Money totalDiscountBase)
    {
        VatRate = vatRate;
        TotalNet = totalNet;
        TotalVat = totalVat;
        TotalGross = totalGross;
        TotalDiscount = totalDiscount;
        TotalNetBase = totalNetBase;
        TotalVatBase = totalVatBase;
        TotalGrossBase = totalGrossBase;
        TotalDiscountBase = totalDiscountBase;
    }
}
