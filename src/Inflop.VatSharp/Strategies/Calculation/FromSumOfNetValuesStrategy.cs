using Inflop.VatSharp.ValueObjects;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Exceptions;
using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.Strategies.Rounding;

namespace Inflop.VatSharp.Strategies.Calculation;

/// <summary>
/// VAT from sum of net values per rate.
/// Art. 226 pts 8, 10 of Directive 2006/112/EC.
///
/// Discounts (art. 79 lit. b of Directive 2006/112/EC) are reflected in the already-reduced
/// <see cref="InvoiceLineItem.TotalNet"/>. The net-equivalent discount amount is
/// carried through to <see cref="LineItemAmounts.DiscountAmount"/> and
/// <see cref="VatRateSummary.TotalDiscount"/> for invoice display purposes.
/// </summary>
internal sealed class FromSumOfNetValuesStrategy : IVatCalculationStrategy
{
    public VatCalculationMethod Method
        => VatCalculationMethod.FromSumOfNetValues;

    public DocumentAmounts Calculate(IReadOnlyList<InvoiceLineItem> lineItems, IRoundingStrategy rounding, IAbsoluteDiscountBehavior discountBehavior)
    {
        if (lineItems is null || lineItems.Count == 0)
            throw new VatCalculationException("At least one line item is required.");

        var itemAmounts = new LineItemAmounts[lineItems.Count];
        var summaries = new List<VatRateSummary>();

        foreach (var group in lineItems.Select((item, i) => (item, i)).GroupBy(x => x.item.VatRate).OrderByDescending(g => g.Key.Percentage))
        {
            var rate = group.Key;
            var sumNet = Money.Zero;
            var sumDiscount = Money.Zero;

            foreach (var (item, originalIndex) in group)
            {
                var (itemNet, itemDiscount) = item.CalculateNetAndDiscount(discountBehavior, rounding);

                sumNet += itemNet;
                sumDiscount += itemDiscount;

                itemAmounts[originalIndex] = LineItemAmounts.FromNet(itemNet, rate, rounding, itemDiscount);
            }

            var sumVat = rate.VatFromNet(sumNet).Round(rounding);
            summaries.Add(new VatRateSummary(rate, sumNet, sumVat, sumNet + sumVat, sumDiscount));
        }

        return new DocumentAmounts(Method, summaries, itemAmounts);
    }

    public VatRateSummaryFcy BuildSummaryFcy(VatRateSummary s, ExchangeRate exchangeRate, IRoundingStrategy baseCurrencyRounding)
    {
        var netBase = exchangeRate.ConvertToBase(s.TotalNet, baseCurrencyRounding);
        var vatBase = s.VatRate.VatFromNet(netBase).Round(baseCurrencyRounding);
        var discountBase = exchangeRate.ConvertToBase(s.TotalDiscount, baseCurrencyRounding);

        return new VatRateSummaryFcy
        (
            vatRate: s.VatRate,
            totalNet: s.TotalNet,
            totalVat: s.TotalVat,
            totalGross: s.TotalGross,
            totalDiscount: s.TotalDiscount,
            totalNetBase: netBase,
            totalVatBase: vatBase,
            totalGrossBase: netBase + vatBase,
            totalDiscountBase: discountBase
        );
    }
}