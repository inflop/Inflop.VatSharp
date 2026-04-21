using Inflop.VatSharp.Strategies.Rounding;

namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// Calculated amounts for a single line item.
/// Immutable result.
/// </summary>
public sealed record LineItemAmounts
{
    /// <summary>
    /// The net value of the line item, i.e. the price before VAT.
    /// </summary>
    public Money NetValue { get; }

    /// <summary>
    /// The VAT amount for the line item.
    /// </summary>
    public Money VatAmount { get; }

    /// <summary>
    /// The gross value of the line item, i.e. the price including VAT.
    /// </summary>
    public Money GrossValue { get; }

    /// <summary>
    /// The VAT rate applicable to the line item.
    /// Used to derive the taxable base and VAT amount.
    /// </summary>
    public VatRate VatRate { get; }

    /// <summary>
    /// Net-equivalent discount applied to this line.
    /// For net-priced items this equals the actual discount amount.
    /// For gross-priced items it is the net equivalent of the gross discount.
    /// Zero when no discount was applied.
    /// </summary>
    public Money DiscountAmount { get; }

    /// <summary>
    /// Creates line item amounts from the net value.
    /// </summary>
    /// <param name="net">The net (pre-VAT) value of the line item.</param>
    /// <param name="rate">The VAT rate applicable to this line.</param>
    /// <param name="roundingStrategy">The rounding strategy applied to the derived VAT and gross amounts.</param>
    /// <param name="discountAmount">Net-equivalent discount. Defaults to zero when omitted.</param>
    public static LineItemAmounts FromNet(Money net, VatRate rate, IRoundingStrategy roundingStrategy, Money discountAmount = default)
    {
        var vat = rate.VatFromNet(net).Round(roundingStrategy);
        return new(net, vat, net + vat, rate, discountAmount);
    }

    /// <summary>
    /// Creates line item amounts from the gross value.
    /// </summary>
    /// <param name="gross">The gross (VAT-inclusive) value of the line item.</param>
    /// <param name="rate">The VAT rate applicable to this line.</param>
    /// <param name="roundingStrategy">The rounding strategy applied to the derived VAT and net amounts.</param>
    /// <param name="discountAmount">Net-equivalent discount. Defaults to zero when omitted.</param>
    public static LineItemAmounts FromGross(Money gross, VatRate rate, IRoundingStrategy roundingStrategy, Money discountAmount = default)
    {
        var vat = rate.VatFromGross(gross).Round(roundingStrategy);
        return new(gross - vat, vat, gross, rate, discountAmount);
    }

    /// <inheritdoc />
    public override string ToString()
        => DiscountAmount.IsZero
            ? $"Net: {NetValue}, VAT {VatRate}: {VatAmount}, Gross: {GrossValue}"
            : $"Net: {NetValue} (discount: {DiscountAmount}), VAT {VatRate}: {VatAmount}, Gross: {GrossValue}";

    private LineItemAmounts(Money net, Money vat, Money gross, VatRate rate, Money discountAmount)
    {
        NetValue = net;
        VatAmount = vat;
        GrossValue = gross;
        VatRate = rate;
        DiscountAmount = discountAmount;
    }
}