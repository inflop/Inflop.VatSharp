using FluentAssertions;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Exceptions;
using Inflop.VatSharp.Mapping;
using Inflop.VatSharp.ValueObjects;
using Xunit;

namespace Inflop.VatSharp.Tests;

// ═══════════════════════════════════════════════════════════════════════════
//  Structural invariants and per-method properties
// ═══════════════════════════════════════════════════════════════════════════

public class InvariantTests
{
    private static readonly InvoiceLineItem[] NetItems =
    [
        new(UnitPrice.Net(9.99m),  Quantity.Of(7), VatRate.Of(23), Discount.OfPercentage(10m)),
        new(UnitPrice.Net(14.50m), Quantity.Of(3), VatRate.Of(23)),
        new(UnitPrice.Net(5.25m),  Quantity.Of(5), VatRate.Of(8),  Discount.OfAmount(2.00m)),
    ];

    private static readonly InvoiceLineItem[] GrossItems =
    [
        new(UnitPrice.Gross(12.29m), Quantity.Of(7), VatRate.Of(23), Discount.OfPercentage(10m)),
        new(UnitPrice.Gross(17.84m), Quantity.Of(3), VatRate.Of(23)),
        new(UnitPrice.Gross(5.67m),  Quantity.Of(5), VatRate.Of(8),  Discount.OfAmount(2.00m)),
    ];

    private static readonly InvoiceLineItem[] ArticleItems =
    [
        new(UnitPrice.Net(4.58m),  Quantity.Of(4), VatRate.Of(23)),
        new(UnitPrice.Net(7.22m),  Quantity.Of(5), VatRate.Of(23)),
        new(UnitPrice.Net(12.74m), Quantity.Of(2), VatRate.Of(8)),
    ];

    private readonly LineItemCalculationEngine<InvoiceLineItem> _engine = VatCalculationEngine.Create();

    public static IEnumerable<object[]> MethodWithCompatibleDataset =>
    [
        [VatCalculationMethod.FromSumOfNetValues,        NetItems],
        [VatCalculationMethod.FromSumOfGrossValues,      GrossItems],
        [VatCalculationMethod.SumOfLineItemVatAmounts,   NetItems],
    ];

    public static IEnumerable<object[]> EachMethod =>
    [
        [VatCalculationMethod.FromSumOfNetValues],
        [VatCalculationMethod.FromSumOfGrossValues],
        [VatCalculationMethod.SumOfLineItemVatAmounts],
    ];

    // ── Niezmienniki zawsze prawdziwe ────────────────────────────────────

    [Theory, MemberData(nameof(MethodWithCompatibleDataset))]
    public void DocumentAmounts_TotalGross_EqualsTotalNetPlusTotalVat(VatCalculationMethod method, InvoiceLineItem[] items)
    {
        var result = _engine.Calculate(items, method);
        result.TotalGross.Should().Be(result.TotalNet + result.TotalVat);
    }

    [Theory, MemberData(nameof(MethodWithCompatibleDataset))]
    public void VatRateSummary_EachSummary_TotalGrossEqualsTotalNetPlusTotalVat(VatCalculationMethod method, InvoiceLineItem[] items)
    {
        var result = _engine.Calculate(items, method);
        result.VatRateSummaries.Should().AllSatisfy(s => s.TotalGross.Should().Be(s.TotalNet + s.TotalVat));
    }

    [Theory, MemberData(nameof(MethodWithCompatibleDataset))]
    public void DocumentAmounts_TotalNet_EqualsSumOfRateSummaryNets(VatCalculationMethod method, InvoiceLineItem[] items)
    {
        var result = _engine.Calculate(items, method);
        var sum = result.VatRateSummaries.Aggregate(Money.Zero, (acc, s) => acc + s.TotalNet);
        sum.Should().Be(result.TotalNet);
    }

    [Theory, MemberData(nameof(MethodWithCompatibleDataset))]
    public void DocumentAmounts_TotalVat_EqualsSumOfRateSummaryVats(VatCalculationMethod method, InvoiceLineItem[] items)
    {
        var result = _engine.Calculate(items, method);
        var sum = result.VatRateSummaries.Aggregate(Money.Zero, (acc, s) => acc + s.TotalVat);
        sum.Should().Be(result.TotalVat);
    }

    [Theory, MemberData(nameof(MethodWithCompatibleDataset))]
    public void DocumentAmounts_TotalDiscount_EqualsSumOfRateSummaryDiscounts(VatCalculationMethod method, InvoiceLineItem[] items)
    {
        var result = _engine.Calculate(items, method);
        var sum = result.VatRateSummaries.Aggregate(Money.Zero, (acc, s) => acc + s.TotalDiscount);
        sum.Should().Be(result.TotalDiscount);
    }

    [Theory, MemberData(nameof(MethodWithCompatibleDataset))]
    public void DocumentAmounts_Method_MatchesRequestedMethod(VatCalculationMethod method, InvoiceLineItem[] items)
    {
        var result = _engine.Calculate(items, method);
        result.Method.Should().Be(method);
    }

    [Theory, MemberData(nameof(MethodWithCompatibleDataset))]
    public void DocumentAmounts_LineItemCount_MatchesInputCount(VatCalculationMethod method, InvoiceLineItem[] items)
    {
        var result = _engine.Calculate(items, method);
        result.LineItems.Should().HaveCount(items.Length);
    }

    // ── Adytywność per metoda ────────────────────────────────────────────

    [Fact]
    public void MethodI_SumOfLineItemNets_EqualsTotalNet()
    {
        var result = _engine.Calculate(NetItems, VatCalculationMethod.FromSumOfNetValues);
        var sum = result.LineItems.Aggregate(Money.Zero, (acc, li) => acc + li.NetValue);
        sum.Should().Be(result.TotalNet);
    }

    [Fact]
    public void MethodII_SumOfLineItemGross_EqualsTotalGross()
    {
        var result = _engine.Calculate(GrossItems, VatCalculationMethod.FromSumOfGrossValues);
        var sum = result.LineItems.Aggregate(Money.Zero, (acc, li) => acc + li.GrossValue);
        sum.Should().Be(result.TotalGross);
    }

    [Fact]
    public void MethodIII_SumOfLineItemVatAmounts_EqualsTotalVat()
    {
        var result = _engine.Calculate(NetItems, VatCalculationMethod.SumOfLineItemVatAmounts);
        var sum = result.LineItems.Aggregate(Money.Zero, (acc, li) => acc + li.VatAmount);
        sum.Should().Be(result.TotalVat);
    }

    [Fact]
    public void MethodIII_SumOfLineItemNets_EqualsTotalNet()
    {
        var result = _engine.Calculate(NetItems, VatCalculationMethod.SumOfLineItemVatAmounts);
        var sum = result.LineItems.Aggregate(Money.Zero, (acc, li) => acc + li.NetValue);
        sum.Should().Be(result.TotalNet);
    }

    // ── Idempotency ──────────────────────────────────────────────────────

    [Theory, MemberData(nameof(MethodWithCompatibleDataset))]
    public void Idempotency_DomesticCalculation_ProducesBitEqualResults(VatCalculationMethod method, InvoiceLineItem[] items)
    {
        var first  = _engine.Calculate(items, method);
        var second = _engine.Calculate(items, method);

        first.Should().BeEquivalentTo(second);
    }

    [Fact]
    public void Idempotency_FcyCalculation_ProducesBitEqualResults()
    {
        var rate = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.2345m, new DateOnly(2024, 10, 21), "NBP");
        var first  = _engine.Calculate(NetItems, VatCalculationMethod.FromSumOfNetValues, rate);
        var second = _engine.Calculate(NetItems, VatCalculationMethod.FromSumOfNetValues, rate);

        first.Should().BeEquivalentTo(second);
    }

    // ── Empty / single-item edge cases ───────────────────────────────────

    [Theory, MemberData(nameof(EachMethod))]
    public void SingleItem_AnyMethod_ProducesValidResult(VatCalculationMethod method)
    {
        var item = method == VatCalculationMethod.FromSumOfGrossValues
            ? new InvoiceLineItem(UnitPrice.Gross(123m), Quantity.One, VatRate.Of(23))
            : new InvoiceLineItem(UnitPrice.Net(100m),   Quantity.One, VatRate.Of(23));

        var result = _engine.Calculate([item], method);

        result.VatRateSummaries.Should().HaveCount(1);
        result.LineItems.Should().HaveCount(1);
        result.TotalGross.Should().Be(result.TotalNet + result.TotalVat);
        result.Method.Should().Be(method);
    }

    [Theory, MemberData(nameof(EachMethod))]
    public void EmptyItems_AnyMethod_Throws(VatCalculationMethod method)
    {
        var act = () => _engine.Calculate([], method);
        act.Should().Throw<VatCalculationException>();
    }

    [Theory, MemberData(nameof(EachMethod))]
    public void SingleItem_ZeroRateOnly_NoVat(VatCalculationMethod method)
    {
        var item = method == VatCalculationMethod.FromSumOfGrossValues
            ? new InvoiceLineItem(UnitPrice.Gross(1000m), Quantity.One, VatRate.Zero)
            : new InvoiceLineItem(UnitPrice.Net(1000m),   Quantity.One, VatRate.Zero);

        var result = _engine.Calculate([item], method);

        result.TotalVat.Value.Should().Be(0m);
        result.TotalNet.Value.Should().Be(1000m);
        result.TotalGross.Value.Should().Be(1000m);
    }
}
