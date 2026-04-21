using Inflop.VatSharp.ValueObjects;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Exceptions;
using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.Strategies.Rounding;

namespace Inflop.VatSharp.Strategies.Calculation;

/// <summary>
/// VAT from sum of gross values: VAT = (Gross × Rate) / (100 + Rate).
/// Art. 226 of Directive 2006/112/EC.
///
/// Discounts are applied in the gross dimension — <see cref="InvoiceLineItem.TotalGross"/>
/// is already the post-discount gross. The net-equivalent discount amount is tracked
/// via <see cref="InvoiceLineItem.DiscountAmountNetWith"/> for summary reporting.
///
/// <para>
/// All line items must carry a gross unit price (<see cref="UnitPrice.IsGross"/> = true).
/// This is a domain invariant: in retail/fiscal scenarios the gross price on the price tag
/// is the canonical fact, not a value derived from a net price.
/// </para>
/// </summary>
/// <exception cref="VatCalculationException">
/// Thrown when any line item has a net unit price.
/// Use <see cref="UnitPrice.Gross(decimal)"/> or switch to
/// <see cref="VatCalculationMethod.FromSumOfNetValues"/>.
/// </exception>
internal sealed class FromSumOfGrossValuesStrategy : IVatCalculationStrategy
{
    public VatCalculationMethod Method
        => VatCalculationMethod.FromSumOfGrossValues;

    public DocumentAmounts Calculate(IReadOnlyList<InvoiceLineItem> lineItems, IRoundingStrategy rounding,
        IAbsoluteDiscountBehavior discountBehavior)
    {
        if (lineItems is null || lineItems.Count == 0)
            throw new VatCalculationException("At least one line item is required.");

        if (lineItems.Any(i => i.UnitPrice.IsNet))
            throw new VatCalculationException("FromSumOfGrossValues requires all items to use UnitPrice.Gross(). Use UnitPrice.Gross() or switch to FromSumOfNetValues strategy.");

        var itemAmounts = new LineItemAmounts[lineItems.Count];
        var summaries = new List<VatRateSummary>();

        foreach (var group in lineItems.Select((item, i) => (item, i)).GroupBy(x => x.item.VatRate).OrderByDescending(g => g.Key.Percentage))
        {
            var rate = group.Key;
            var sumGross = Money.Zero;
            var sumDiscount = Money.Zero;

            foreach (var (item, originalIndex) in group)
            {
                var itemGross = item.TotalGrossWith(discountBehavior, rounding).Round(rounding);
                var itemDiscount = item.DiscountAmountNetWith(discountBehavior, rounding).Round(rounding);

                sumGross += itemGross;
                sumDiscount += itemDiscount;

                itemAmounts[originalIndex] = LineItemAmounts.FromGross(itemGross, rate, rounding, itemDiscount);
            }

            var sumVat = rate.VatFromGross(sumGross).Round(rounding);
            var sumNet = sumGross - sumVat;
            summaries.Add(new VatRateSummary(rate, sumNet, sumVat, sumGross, sumDiscount));
        }

        return new DocumentAmounts(Method, summaries, itemAmounts);
    }

    public VatRateSummaryFcy BuildSummaryFcy(VatRateSummary s, ExchangeRate exchangeRate, IRoundingStrategy baseCurrencyRounding)
    {
        var grossBase = exchangeRate.ConvertToBase(s.TotalGross, baseCurrencyRounding);
        var vatBase = s.VatRate.VatFromGross(grossBase).Round(baseCurrencyRounding);
        var discountBase = exchangeRate.ConvertToBase(s.TotalDiscount, baseCurrencyRounding);

        return new VatRateSummaryFcy
        (
            vatRate: s.VatRate,
            totalNet: s.TotalNet,
            totalVat: s.TotalVat,
            totalGross: s.TotalGross,
            totalDiscount: s.TotalDiscount,
            totalNetBase: grossBase - vatBase,
            totalVatBase: vatBase,
            totalGrossBase: grossBase,
            totalDiscountBase: discountBase
        );
    }
}
