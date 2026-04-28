using Inflop.VatSharp.ValueObjects;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Exceptions;
using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.Strategies.Rounding;

namespace Inflop.VatSharp.Strategies.Calculation;

/// <summary>
/// Sum of per-line-item VAT amounts.
/// Art. 226 pt 10 of Directive 2006/112/EC.
/// May produce rounding differences vs Method I due to per-line rounding.
///
/// Discounts are reflected in the already-reduced <see cref="InvoiceLineItem.TotalNet"/>.
/// The net-equivalent discount is tracked in each <see cref="LineItemAmounts.DiscountAmount"/>
/// and aggregated per rate in <see cref="VatRateSummary.TotalDiscount"/>.
/// </summary>
internal sealed class SumOfLineItemVatAmountsStrategy : IVatCalculationStrategy
{
    public VatCalculationMethod Method
        => VatCalculationMethod.SumOfLineItemVatAmounts;

    public DocumentAmounts Calculate(IReadOnlyList<InvoiceLineItem> lineItems, IRoundingStrategy rounding, IAbsoluteDiscountBehavior discountBehavior)
    {
        if (lineItems is null || lineItems.Count == 0)
            throw new VatCalculationException("At least one line item is required.");

        var itemAmounts = lineItems
            .Select(item =>
            {
                var (net, discount) = item.CalculateNetAndDiscount(discountBehavior, rounding);
                return LineItemAmounts.FromNet(net, item.VatRate, rounding, discount);
            })
            .ToList();

        var summaries = itemAmounts
            .GroupBy(a => a.VatRate)
            .OrderByDescending(g => g.Key.Percentage)
            .Select(g => new VatRateSummary
            (
                vatRate: g.Key,
                totalNet: g.Aggregate(Money.Zero, (acc, a) => acc + a.NetValue),
                totalVat: g.Aggregate(Money.Zero, (acc, a) => acc + a.VatAmount),
                totalGross: g.Aggregate(Money.Zero, (acc, a) => acc + a.GrossValue),
                totalDiscount: g.Aggregate(Money.Zero, (acc, a) => acc + a.DiscountAmount)
            ))
            .ToList();

        return new DocumentAmounts(Method, summaries, itemAmounts);
    }

    public VatRateSummaryFcy BuildSummaryFcy(VatRateSummary s, ExchangeRate exchangeRate, IRoundingStrategy baseCurrencyRounding)
    {
        var netBase = exchangeRate.ConvertToBase(s.TotalNet, baseCurrencyRounding);
        var vatBase = exchangeRate.ConvertToBase(s.TotalVat, baseCurrencyRounding);
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