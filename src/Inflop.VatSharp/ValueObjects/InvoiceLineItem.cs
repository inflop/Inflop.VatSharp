using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.Strategies.Rounding;

namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// Line item input data. Does not store calculated amounts.
///
/// The optional <see cref="Discount"/> reduces the taxable base per
/// art. 79 lit. b of Directive 2006/112/EC.
/// </summary>
/// <param name="UnitPrice">
/// Unit price in the input price type:
/// net for net-priced items, gross for gross-priced items.
/// </param>
/// <param name="Quantity">
/// Quantity of items. The unit is not relevant for VAT calculation,
/// only the numeric value.
/// </param>
/// <param name="VatRate">
/// VAT rate applicable to the line item. Used to derive the taxable base and VAT amount.
/// </param>
/// <param name="Discount">
/// Optional price reduction. When present it is applied to the line total
/// (UnitPrice × Quantity) in the same price type as the unit price.
/// Null means no discount — the full unit price is the taxable basis.
/// </param>
public sealed record InvoiceLineItem(UnitPrice UnitPrice, Quantity Quantity, VatRate VatRate, Discount? Discount = null)
{
    /// <summary>
    /// Price × Qty in the input price type, <em>before</em> any discount.
    /// Used as the base for <see cref="DiscountAmount"/> calculation.
    /// </summary>
    public Money TotalInInputType
        => UnitPrice.Amount * Quantity;

    /// <summary>
    /// Net value <em>before</em> any discount.
    /// Used to derive discount net amount for VAT summaries.
    /// </summary>
    public Money TotalNetBeforeDiscount
        => UnitPrice.IsNet
            ? UnitPrice.Amount * Quantity
            : VatRate.NetFromGross(UnitPrice.Amount * Quantity);

    /// <summary>
    /// Discount amount in the <em>input</em> price type:
    /// net for net-priced items, gross for gross-priced items.
    /// Zero when no discount is defined.
    /// </summary>
    public Money DiscountAmount
        => Discount?.CalculateFrom(TotalInInputType) ?? Money.Zero;

    /// <summary>
    /// Total net value <em>after</em> discount — the actual taxable base.
    /// </summary>
    public Money TotalNet
        => UnitPrice.IsNet
            ? UnitPrice.Amount * Quantity - DiscountAmount
            : VatRate.NetFromGross(UnitPrice.Amount * Quantity - DiscountAmount);

    /// <summary>
    /// Total gross value <em>after</em> discount.
    /// </summary>
    public Money TotalGross
        => UnitPrice.IsGross
            ? UnitPrice.Amount * Quantity - DiscountAmount
            : VatRate.GrossFromNet(TotalNet);

    internal Money DiscountAmountNetWith(IAbsoluteDiscountBehavior discountBehavior, IRoundingStrategy rounding)
        => Discount.HasValue
            ? TotalNetBeforeDiscount - TotalNetWith(discountBehavior, rounding)
            : Money.Zero;

    internal Money TotalNetWith(IAbsoluteDiscountBehavior discountBehavior, IRoundingStrategy rounding)
    {
        if (!HasAbsoluteDiscount)
            return TotalNet;

        var totalInInputType = discountBehavior.TotalInInputType(UnitPrice.Amount, Quantity, DiscountAmount, rounding);
        return UnitPrice.IsNet
            ? totalInInputType
            : VatRate.NetFromGross(totalInInputType);
    }

    internal Money TotalGrossWith(IAbsoluteDiscountBehavior discountBehavior, IRoundingStrategy rounding)
    {
        if (!HasAbsoluteDiscount)
            return TotalGross;

        var totalInInputType = discountBehavior.TotalInInputType(UnitPrice.Amount, Quantity, DiscountAmount, rounding);
        return UnitPrice.IsGross
            ? totalInInputType
            : VatRate.GrossFromNet(totalInInputType);
    }

    /// <summary>
    /// Returns rounded net and net-equivalent discount in a single call,
    /// avoiding a redundant second invocation of <see cref="TotalNetWith"/>.
    /// </summary>
    internal (Money Net, Money Discount) CalculateNetAndDiscount(IAbsoluteDiscountBehavior discountBehavior, IRoundingStrategy rounding)
    {
        var net = TotalNetWith(discountBehavior, rounding);
        var discount = Discount.HasValue ? TotalNetBeforeDiscount - net : Money.Zero;
        return (net.Round(rounding), discount.Round(rounding));
    }

    /// <summary>
    /// Returns rounded gross and net-equivalent discount in a single call,
    /// avoiding a redundant second invocation of <see cref="TotalGrossWith"/> via
    /// <see cref="DiscountAmountNetWith"/>.
    /// </summary>
    internal (Money Gross, Money NetDiscount) CalculateGrossAndNetDiscount(IAbsoluteDiscountBehavior discountBehavior, IRoundingStrategy rounding)
    {
        var gross = TotalGrossWith(discountBehavior, rounding);
        var netDiscount = Discount.HasValue
            ? TotalNetBeforeDiscount - VatRate.NetFromGross(gross)
            : Money.Zero;
        return (gross.Round(rounding), netDiscount.Round(rounding));
    }

    internal LineItemAmounts Calculate(IRoundingStrategy rounding, IAbsoluteDiscountBehavior discountBehavior)
    {
        var (net, discount) = CalculateNetAndDiscount(discountBehavior, rounding);
        return LineItemAmounts.FromNet(net, VatRate, rounding, discount);
    }

    private bool HasAbsoluteDiscount
        => Discount is { Type: DiscountType.Absolute, IsZero: false };
}
