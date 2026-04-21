using FluentAssertions;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Mapping;
using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.Strategies.Rounding;
using Inflop.VatSharp.ValueObjects;
using Xunit;

namespace Inflop.VatSharp.Tests;

// ═══════════════════════════════════════════════════════════════════════════
//  Discount value object
// ═══════════════════════════════════════════════════════════════════════════

public class DiscountTests
{
    // ── Factory ────────────────────────────────────────────────────────────

    [Fact]
    public void OfAmount_PositiveValue_CreatesAbsoluteDiscount()
    {
        var d = Discount.OfAmount(5m);
        d.Type.Should().Be(DiscountType.Absolute);
        d.AbsoluteAmount.Value.Should().Be(5m);
    }

    [Fact]
    public void OfAmount_Zero_CreatesZeroDiscount()
    {
        var d = Discount.OfAmount(0m);
        d.IsZero.Should().BeTrue();
    }

    [Fact]
    public void OfAmount_Negative_Throws()
        => FluentActions.Invoking(() => Discount.OfAmount(-1m))
            .Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void OfPercentage_ValidRange_CreatesPercentageDiscount()
    {
        var d = Discount.OfPercentage(10m);
        d.Type.Should().Be(DiscountType.Percentage);
        d.Percentage.Should().Be(10m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void OfPercentage_BoundaryValues_Valid(decimal pct)
        => FluentActions.Invoking(() => Discount.OfPercentage(pct))
            .Should().NotThrow();

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void OfPercentage_OutOfRange_Throws(decimal pct)
        => FluentActions.Invoking(() => Discount.OfPercentage(pct))
            .Should().Throw<ArgumentOutOfRangeException>();

    // ── CalculateFrom ──────────────────────────────────────────────────────

    [Fact]
    public void CalculateFrom_AbsoluteDiscount_ReturnsFixedAmount()
    {
        var d = Discount.OfAmount(5m);
        d.CalculateFrom(Money.Of(40m)).Value.Should().Be(5m);
    }

    [Fact]
    public void CalculateFrom_PercentageDiscount_ComputesFraction()
    {
        var d = Discount.OfPercentage(10m);
        d.CalculateFrom(Money.Of(40m)).Value.Should().Be(4m);   // 40 × 10% = 4
    }

    [Fact]
    public void CalculateFrom_PercentageDiscount_FractionalResult()
    {
        // 10% of 33.33 = 3.333 — unrounded, caller rounds
        var d = Discount.OfPercentage(10m);
        d.CalculateFrom(Money.Of(33.33m)).Value.Should().Be(3.333m);
    }

    [Fact]
    public void CalculateFrom_AbsoluteExceedsBase_Throws()
        => FluentActions.Invoking(() => Discount.OfAmount(50m).CalculateFrom(Money.Of(40m)))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*exceeds base amount*");

    [Fact]
    public void CalculateFrom_AbsoluteEqualsBase_ReturnsFullAmount()
    {
        var d = Discount.OfAmount(40m);
        d.CalculateFrom(Money.Of(40m)).Value.Should().Be(40m);  // 100% off — valid edge case
    }

    // ── Wrong-kind accessors ───────────────────────────────────────────────

    [Fact]
    public void Percentage_OnAbsoluteDiscount_Throws()
        => FluentActions.Invoking(() => _ = Discount.OfAmount(5m).Percentage)
            .Should().Throw<InvalidOperationException>();

    [Fact]
    public void AbsoluteAmount_OnPercentageDiscount_Throws()
        => FluentActions.Invoking(() => _ = Discount.OfPercentage(10m).AbsoluteAmount)
            .Should().Throw<InvalidOperationException>();
}

// ═══════════════════════════════════════════════════════════════════════════
//  InvoiceLineItem discount properties
// ═══════════════════════════════════════════════════════════════════════════

public class InvoiceLineItemDiscountTests
{
    // ── Net-priced items ───────────────────────────────────────────────────

    [Fact]
    public void TotalNet_NetPrice_WithPercentageDiscount()
    {
        // 4 szt. × 10.00 zł netto, rabat 10%  →  (40 − 4) = 36.00 netto
        var item = new InvoiceLineItem(
            UnitPrice.Net(10m),
            Quantity.Of(4),
            VatRate.Of(23),
            Discount.OfPercentage(10m));

        item.TotalNet.Value.Should().Be(36m);
    }

    [Fact]
    public void TotalNet_NetPrice_WithAbsoluteDiscount()
    {
        // 4 szt. × 10.00 zł netto, rabat 5.00 zł  →  (40 − 5) = 35.00 netto
        var item = new InvoiceLineItem(
            UnitPrice.Net(10m),
            Quantity.Of(4),
            VatRate.Of(23),
            Discount.OfAmount(5m));

        item.TotalNet.Value.Should().Be(35m);
    }

    [Fact]
    public void DiscountAmount_NetPrice_IsInNetTerms()
    {
        var item = new InvoiceLineItem(
            UnitPrice.Net(10m),
            Quantity.Of(4),
            VatRate.Of(23),
            Discount.OfPercentage(10m));

        item.DiscountAmount.Value.Should().Be(4m);
        item.DiscountAmountNetWith(FromTotalAbsoluteDiscountBehavior.Instance, DefaultRounding.TwoDecimalPlaces)
            .Value.Should().Be(4m);
    }

    [Fact]
    public void TotalNetBeforeDiscount_NetPrice()
    {
        var item = new InvoiceLineItem(
            UnitPrice.Net(10m),
            Quantity.Of(4),
            VatRate.Of(23),
            Discount.OfPercentage(10m));

        item.TotalNetBeforeDiscount.Value.Should().Be(40m);
    }

    [Fact]
    public void NoDiscount_DiscountAmountIsZero()
    {
        var item = new InvoiceLineItem(UnitPrice.Net(10m), Quantity.Of(4), VatRate.Of(23));

        item.DiscountAmount.Value.Should().Be(0m);
        item.DiscountAmountNetWith(FromTotalAbsoluteDiscountBehavior.Instance, DefaultRounding.TwoDecimalPlaces)
            .Value.Should().Be(0m);
    }

    // ── Gross-priced items ─────────────────────────────────────────────────

    [Fact]
    public void TotalGross_GrossPrice_WithPercentageDiscount()
    {
        // 1 szt. × 123.00 brutto, rabat 10%  →  123 − 12.30 = 110.70 brutto
        var item = new InvoiceLineItem(
            UnitPrice.Gross(123m),
            Quantity.One,
            VatRate.Of(23),
            Discount.OfPercentage(10m));

        item.TotalGross.Value.Should().Be(110.70m);
    }

    [Fact]
    public void TotalNet_GrossPrice_WithPercentageDiscount()
    {
        // EffectiveGross = 123 × 0.90 = 110.70
        // NetFromGross(110.70) = 110.70 − (110.70 × 23/123) = 110.70 − 20.70 = 90.00
        // (Because 123 × 0.90 is exactly divisible: 110.70 × 23/123 = 20.70 exactly)
        var item = new InvoiceLineItem(
            UnitPrice.Gross(123m),
            Quantity.One,
            VatRate.Of(23),
            Discount.OfPercentage(10m));

        item.TotalNet.Value.Should().Be(90.00m);
    }

    [Fact]
    public void DiscountAmountNetWith_GrossPrice_IsNetEquivalent()
    {
        var item = new InvoiceLineItem(
            UnitPrice.Gross(123m),
            Quantity.One,
            VatRate.Of(23),
            Discount.OfPercentage(10m));

        item.TotalNetBeforeDiscount.Value.Should().Be(100m);
        item.DiscountAmountNetWith(FromTotalAbsoluteDiscountBehavior.Instance, DefaultRounding.TwoDecimalPlaces)
            .Value.Should().Be(10m);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  End-to-end: discounts through calculation strategies
// ═══════════════════════════════════════════════════════════════════════════

public class DiscountCalculationTests
{
    private readonly LineItemCalculationEngine<InvoiceLineItem> _calc = VatCalculationEngine.Create();

    // ── Method I — percentage discount ────────────────────────────────────

    [Fact]
    public void MethodI_PercentageDiscount_ReducesTaxableBase()
    {
        // Towar: 4 szt. × 10.00 netto, 23%, rabat 10%
        //   Net before: 40.00
        //   Discount:    4.00  (10%)
        //   Net after:  36.00
        //   VAT:        36.00 × 23% = 8.28
        //   Gross:      44.28
        var item = new InvoiceLineItem(
            UnitPrice.Net(10m),
            Quantity.Of(4),
            VatRate.Of(23),
            Discount.OfPercentage(10m));

        var result = _calc.Calculate([item], VatCalculationMethod.FromSumOfNetValues);

        result.TotalNet.Value.Should().Be(36m);
        result.TotalVat.Value.Should().Be(8.28m);
        result.TotalGross.Value.Should().Be(44.28m);
        result.TotalDiscount.Value.Should().Be(4m);

        var summary = result.VatRateSummaries.Single();
        summary.TotalDiscount.Value.Should().Be(4m);
    }

    [Fact]
    public void MethodI_AbsoluteDiscount_ReducesTaxableBase()
    {
        // Towar: 4 szt. × 10.00 netto, 23%, rabat 5.00 zł
        //   Net before: 40.00
        //   Discount:    5.00
        //   Net after:  35.00
        //   VAT:        35.00 × 23% = 8.05
        //   Gross:      43.05
        var item = new InvoiceLineItem(
            UnitPrice.Net(10m),
            Quantity.Of(4),
            VatRate.Of(23),
            Discount.OfAmount(5m));

        var result = _calc.Calculate([item], VatCalculationMethod.FromSumOfNetValues);

        result.TotalNet.Value.Should().Be(35m);
        result.TotalVat.Value.Should().Be(8.05m);
        result.TotalGross.Value.Should().Be(43.05m);
        result.TotalDiscount.Value.Should().Be(5m);
    }

    // ── Method I — mixed discount/no-discount lines ────────────────────────

    [Fact]
    public void MethodI_MixedLines_DiscountOnlySomeLines()
    {
        // Linia 1: 2 szt. × 20.00 netto, 23%  — bez rabatu  = 40.00 netto
        // Linia 2: 1 szt. × 30.00 netto, 23%, rabat 10%     = 27.00 netto
        //   Suma netto:    67.00
        //   VAT 23%:       67.00 × 23% = 15.41
        //   Suma brutto:   82.41
        //   TotalDiscount: 3.00
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(20m), Quantity.Of(2), VatRate.Of(23)),
            new(UnitPrice.Net(30m), Quantity.One,   VatRate.Of(23), Discount.OfPercentage(10m)),
        };

        var result = _calc.Calculate(items, VatCalculationMethod.FromSumOfNetValues);

        result.TotalNet.Value.Should().Be(67m);
        result.TotalVat.Value.Should().Be(15.41m);
        result.TotalGross.Value.Should().Be(82.41m);
        result.TotalDiscount.Value.Should().Be(3m);
    }

    // ── Method I — multi-rate with discounts ──────────────────────────────

    [Fact]
    public void MethodI_MultiRate_DiscountTrackedPerRate()
    {
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(100m), Quantity.One, VatRate.Of(23), Discount.OfPercentage(10m)),
            new(UnitPrice.Net(50m),  Quantity.One, VatRate.Of(8),  Discount.OfAmount(5m)),
        };

        var result = _calc.Calculate(items, VatCalculationMethod.FromSumOfNetValues);

        var vat23 = result.VatRateSummaries.Single(s => s.VatRate == VatRate.Of(23));
        var vat8 = result.VatRateSummaries.Single(s => s.VatRate == VatRate.Of(8));

        vat23.TotalDiscount.Value.Should().Be(10m);   // 10% of 100
        vat8.TotalDiscount.Value.Should().Be(5m);     // absolute 5

        result.TotalDiscount.Value.Should().Be(15m);
    }

    // ── Method II — gross-priced with discount ────────────────────────────

    [Fact]
    public void MethodII_GrossPrice_PercentageDiscount()
    {
        // 1 szt. × 123.00 brutto, 23%, rabat 10%
        //   Gross before:  123.00 → Gross after: 110.70
        //   VatFromGross:  110.70 × 23/123 = 20.70  (exact — 123 × 0.9 divides cleanly)
        //   Net:           110.70 − 20.70 = 90.00
        //   DiscountAmountNet: NetFromGross(123) − NetFromGross(110.70) = 100 − 90 = 10.00
        var item = new InvoiceLineItem(
            UnitPrice.Gross(123m),
            Quantity.One,
            VatRate.Of(23),
            Discount.OfPercentage(10m));

        var result = _calc.Calculate([item], VatCalculationMethod.FromSumOfGrossValues);

        var summary = result.VatRateSummaries.Single();
        summary.TotalGross.Value.Should().Be(110.70m);
        summary.TotalVat.Value.Should().Be(20.70m);
        summary.TotalNet.Value.Should().Be(90.00m);
        summary.TotalDiscount.Value.Should().Be(10m);
        result.TotalDiscount.Value.Should().Be(10m);
    }

    // ── Method III — per-line discount propagation ─────────────────────────

    [Fact]
    public void MethodIII_PerLine_DiscountInEachLineItemAmounts()
    {
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(10m), Quantity.Of(4), VatRate.Of(23), Discount.OfPercentage(10m)),
            new(UnitPrice.Net(20m), Quantity.Of(2), VatRate.Of(23)),
        };

        var result = _calc.Calculate(items, VatCalculationMethod.SumOfLineItemVatAmounts);

        result.LineItems[0].DiscountAmount.Value.Should().Be(4m);   // 10% of 40 = 4
        result.LineItems[1].DiscountAmount.Value.Should().Be(0m);   // no discount

        result.TotalDiscount.Value.Should().Be(4m);
    }

    // ── CalculateLineItem ──────────────────────────────────────────────────

    [Fact]
    public void CalculateLineItem_WithDiscount_ReducedNet()
    {
        // 5 szt. × 6.23 netto, 23%, rabat 5%
        //   Total = 31.15, discount = 31.15 × 5% = 1.5575
        //   Net = 31.15 − 1.5575 = 29.5925 → rounded 29.59
        //   VAT = 29.59 × 23% = 6.8057 → 6.81
        //   Gross = 29.59 + 6.81 = 36.40
        //   DiscountAmount = 31.15 − 29.59 = 1.56
        var item = new InvoiceLineItem(
            UnitPrice.Net(6.23m),
            Quantity.Of(5),
            VatRate.Of(23),
            Discount.OfPercentage(5m));

        var amounts = _calc.CalculateLineItem(item);

        amounts.NetValue.Value.Should().Be(29.59m);
        amounts.VatAmount.Value.Should().Be(6.81m);
        amounts.GrossValue.Value.Should().Be(36.40m);
        amounts.DiscountAmount.Value.Should().Be(1.56m);
    }

    // ── Zero discount ──────────────────────────────────────────────────────

    [Fact]
    public void ZeroDiscount_BehavesIdenticallyToNoDiscount()
    {
        var withZero = new InvoiceLineItem(
            UnitPrice.Net(10m), Quantity.Of(4), VatRate.Of(23), Discount.OfAmount(0m));
        var withNone = new InvoiceLineItem(
            UnitPrice.Net(10m), Quantity.Of(4), VatRate.Of(23));

        var r1 = _calc.Calculate([withZero], VatCalculationMethod.FromSumOfNetValues);
        var r2 = _calc.Calculate([withNone], VatCalculationMethod.FromSumOfNetValues);

        r1.TotalNet.Should().Be(r2.TotalNet);
        r1.TotalVat.Should().Be(r2.TotalVat);
        r1.TotalDiscount.Value.Should().Be(0m);
    }

    // ── Backward compatibility ─────────────────────────────────────────────

    [Fact]
    public void ExistingArticleData_WithoutDiscount_Unchanged()
    {
        // Existing test data from VatCalculatorTests — must still pass
        InvoiceLineItem[] articleItems =
        [
            new(UnitPrice.Net(4.58m), Quantity.Of(4), VatRate.Of(23)),
            new(UnitPrice.Net(7.22m), Quantity.Of(5), VatRate.Of(23)),
            new(UnitPrice.Net(12.74m), Quantity.Of(2), VatRate.Of(8)),
        ];

        var result = _calc.Calculate(articleItems, VatCalculationMethod.FromSumOfNetValues);

        result.TotalNet.Value.Should().Be(79.90m);
        result.TotalVat.Value.Should().Be(14.56m);
        result.TotalGross.Value.Should().Be(94.46m);
        result.TotalDiscount.Value.Should().Be(0m);  // no discounts
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Fluent mapping engine with discount
// ═══════════════════════════════════════════════════════════════════════════

public class DiscountMappingEngineTests
{
    // DTOs scoped to this test class — no changes to production models required.
    private record PositionDto(decimal Price, int Qty, int Vat, decimal? DiscountPct);
    private record PositionWithAbsoluteDiscountDto(decimal Price, int Qty, int Vat, decimal Discount);

    [Fact]
    public void Engine_DiscountPercentage_MappedCorrectly()
    {
        var engine = VatCalculationEngine.ForItems<PositionDto>(cfg => cfg
            .NetUnitPrice(p => p.Price)
            .Quantity(p => p.Qty)
            .VatRate(p => p.Vat)
            .DiscountPercentage(p => p.DiscountPct));

        var lines = new[]
        {
            new PositionDto(10m, 4, 23, 10m),   // 40 net, -10% = 36 net
            new PositionDto(20m, 1, 23, null),   // 20 net, no discount
        };

        var result = engine.Calculate(lines, VatCalculationMethod.FromSumOfNetValues);

        result.TotalNet.Value.Should().Be(56m);       // 36 + 20
        result.TotalDiscount.Value.Should().Be(4m);   // only first line has discount
    }

    [Fact]
    public void Engine_DiscountAbsolute_MappedCorrectly()
    {
        var engine = VatCalculationEngine.ForItems<PositionWithAbsoluteDiscountDto>(cfg => cfg
            .NetUnitPrice(p => p.Price)
            .Quantity(p => p.Qty)
            .VatRate(p => p.Vat)
            .DiscountAbsolute(p => p.Discount));

        var lines = new[] { new PositionWithAbsoluteDiscountDto(10m, 4, 23, 5m) };
        var result = engine.Calculate(lines, VatCalculationMethod.FromSumOfNetValues);

        result.TotalNet.Value.Should().Be(35m);
        result.TotalDiscount.Value.Should().Be(5m);
    }

    [Fact]
    public void Engine_NoDiscountConfigured_TotalDiscountIsZero()
    {
        // DiscountPercentage deliberately omitted — engine should still work;
        // all lines treated as having no discount.
        var engine = VatCalculationEngine.ForItems<PositionDto>(cfg => cfg
            .NetUnitPrice(p => p.Price)
            .Quantity(p => p.Qty)
            .VatRate(p => p.Vat));

        var lines = new[] { new PositionDto(10m, 4, 23, 10m) };
        var result = engine.Calculate(lines, VatCalculationMethod.FromSumOfNetValues);

        result.TotalDiscount.Value.Should().Be(0m);
        result.TotalNet.Value.Should().Be(40m);   // discount not applied — correct
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  PerUnit absolute discount behavior — integration tests
// ═══════════════════════════════════════════════════════════════════════════

public class PerUnitAbsoluteDiscountTests
{
    private readonly LineItemCalculationEngine<InvoiceLineItem> _perUnit =
        VatCalculationEngine.Create(discountBehavior: PerUnitAbsoluteDiscountBehavior.Instance);

    private readonly LineItemCalculationEngine<InvoiceLineItem> _fromTotal =
        VatCalculationEngine.Create();

    [Fact]
    public void MethodI_PerUnit_NonDivisibleQuantity_CorrectDiscountAndReconciliation()
    {
        // 3 szt. × 10.00 netto, 23%, rabat absolutny 5.00
        //   discountPerUnit = round(5.00 / 3) = 1.67
        //   net = (10.00 − 1.67) × 3 = 24.99
        //   discount = 30.00 − 24.99 = 5.01
        //   VAT = 24.99 × 23% = 5.75 (rounded)
        //   gross = 30.74
        var item = new InvoiceLineItem(
            UnitPrice.Net(10m), Quantity.Of(3), VatRate.Of(23), Discount.OfAmount(5m));

        var result = _perUnit.Calculate([item], VatCalculationMethod.FromSumOfNetValues);

        result.TotalNet.Value.Should().Be(24.99m);
        result.TotalDiscount.Value.Should().Be(5.01m);
        result.TotalVat.Value.Should().Be(5.75m);
        result.TotalGross.Value.Should().Be(30.74m);

        (result.TotalNet.Value + result.TotalDiscount.Value).Should().Be(30.00m);
    }

    [Fact]
    public void MethodI_PerUnit_DivisibleQuantity_MatchesFromTotal()
    {
        // 4 szt. × 10.00 netto, 23%, rabat absolutny 4.00
        //   discountPerUnit = round(4.00 / 4) = 1.00 — divides evenly
        //   net = (10.00 − 1.00) × 4 = 36.00  — same as FromTotal: 40 − 4 = 36
        var item = new InvoiceLineItem(
            UnitPrice.Net(10m), Quantity.Of(4), VatRate.Of(23), Discount.OfAmount(4m));

        var perUnit = _perUnit.Calculate([item], VatCalculationMethod.FromSumOfNetValues);
        var fromTotal = _fromTotal.Calculate([item], VatCalculationMethod.FromSumOfNetValues);

        perUnit.TotalNet.Should().Be(fromTotal.TotalNet);
        perUnit.TotalVat.Should().Be(fromTotal.TotalVat);
        perUnit.TotalDiscount.Should().Be(fromTotal.TotalDiscount);
    }

    [Fact]
    public void MethodII_PerUnit_GrossPrice_CorrectDiscount()
    {
        // 3 szt. × 12.30 brutto, 23%, rabat absolutny 5.00
        //   discountPerUnit = round(5.00 / 3) = 1.67
        //   gross = (12.30 − 1.67) × 3 = 31.89
        //   VAT = VatFromGross(31.89) = 31.89 × 23/123 = 5.96 (rounded)
        //   net = 31.89 − 5.96 = 25.93
        var item = new InvoiceLineItem(
            UnitPrice.Gross(12.30m), Quantity.Of(3), VatRate.Of(23), Discount.OfAmount(5m));

        var result = _perUnit.Calculate([item], VatCalculationMethod.FromSumOfGrossValues);

        result.TotalGross.Value.Should().Be(31.89m);
        result.TotalVat.Value.Should().Be(5.96m);
        result.TotalNet.Value.Should().Be(25.93m);
    }

    [Fact]
    public void MethodIII_PerUnit_NonDivisibleQuantity_PerLineVat()
    {
        // 3 szt. × 10.00 netto, 23%, rabat absolutny 5.00
        //   net = 24.99 (same as Method I)
        //   VAT per line = 24.99 × 23% = 5.75 (rounded per line)
        var item = new InvoiceLineItem(
            UnitPrice.Net(10m), Quantity.Of(3), VatRate.Of(23), Discount.OfAmount(5m));

        var result = _perUnit.Calculate([item], VatCalculationMethod.SumOfLineItemVatAmounts);

        result.TotalNet.Value.Should().Be(24.99m);
        result.TotalDiscount.Value.Should().Be(5.01m);
    }

    [Fact]
    public void PercentageDiscount_UnaffectedByBehavior()
    {
        var item = new InvoiceLineItem(
            UnitPrice.Net(10m), Quantity.Of(3), VatRate.Of(23), Discount.OfPercentage(10m));

        var perUnit = _perUnit.Calculate([item], VatCalculationMethod.FromSumOfNetValues);
        var fromTotal = _fromTotal.Calculate([item], VatCalculationMethod.FromSumOfNetValues);

        perUnit.TotalNet.Should().Be(fromTotal.TotalNet);
        perUnit.TotalVat.Should().Be(fromTotal.TotalVat);
        perUnit.TotalDiscount.Should().Be(fromTotal.TotalDiscount);
    }

    [Fact]
    public void PerUnit_DiscountPerUnitRoundsUpPastUnitPrice_ClampedToZero()
    {
        // unitPrice = 1.006 (3dp — common in wholesale), qty = 2, discount = 2.01
        //   discount/qty = 1.005 → round(1.005, 2dp, AwayFromZero) = 1.01 > 1.006
        //   Without fix: Money.Of(1.006 − 1.01) throws ArgumentOutOfRangeException
        //   With fix: effectiveUnitPrice clamped to 0 → total = 0
        var item = new InvoiceLineItem(
            UnitPrice.Net(1.006m), Quantity.Of(2), VatRate.Of(23), Discount.OfAmount(2.01m));

        var result = _perUnit.Calculate([item], VatCalculationMethod.FromSumOfNetValues);

        result.TotalNet.Value.Should().Be(0m);
        result.TotalVat.Value.Should().Be(0m);
        result.TotalGross.Value.Should().Be(0m);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  100 % discount end-to-end
// ═══════════════════════════════════════════════════════════════════════════

public class FullDiscountTests
{
    [Fact]
    public void MethodI_100PercentDiscount_AllAmountsZero()
    {
        var engine = VatCalculationEngine.Create();
        var item = new InvoiceLineItem(
            UnitPrice.Net(100m), Quantity.One, VatRate.Of(23), Discount.OfPercentage(100m));

        var result = engine.Calculate([item], VatCalculationMethod.FromSumOfNetValues);

        result.TotalNet.Value.Should().Be(0m);
        result.TotalVat.Value.Should().Be(0m);
        result.TotalGross.Value.Should().Be(0m);
        result.TotalDiscount.Value.Should().Be(100m);
    }

    [Fact]
    public void MethodII_100PercentDiscount_AllAmountsZero()
    {
        var engine = VatCalculationEngine.Create();
        var item = new InvoiceLineItem(
            UnitPrice.Gross(123m), Quantity.One, VatRate.Of(23), Discount.OfPercentage(100m));

        var result = engine.Calculate([item], VatCalculationMethod.FromSumOfGrossValues);

        result.TotalNet.Value.Should().Be(0m);
        result.TotalVat.Value.Should().Be(0m);
        result.TotalGross.Value.Should().Be(0m);
    }

    [Fact]
    public void MethodIII_100PercentDiscount_AllAmountsZero()
    {
        var engine = VatCalculationEngine.Create();
        var item = new InvoiceLineItem(
            UnitPrice.Net(100m), Quantity.One, VatRate.Of(23), Discount.OfPercentage(100m));

        var result = engine.Calculate([item], VatCalculationMethod.SumOfLineItemVatAmounts);

        result.TotalNet.Value.Should().Be(0m);
        result.TotalVat.Value.Should().Be(0m);
        result.TotalGross.Value.Should().Be(0m);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Method II with absolute discount
// ═══════════════════════════════════════════════════════════════════════════

public class MethodIIAbsoluteDiscountTests
{
    [Fact]
    public void MethodII_GrossPrice_AbsoluteDiscount_ReducesTaxableBase()
    {
        // 1 szt. × 123.00 brutto, 23%, rabat 10.00 zł (absolute)
        //   Gross before: 123.00 → Gross after: 113.00
        //   VatFromGross: 113.00 × 23/123 = 21.13008 → 21.13 (rounded)
        //   Net: 113.00 − 21.13 = 91.87
        var engine = VatCalculationEngine.Create();
        var item = new InvoiceLineItem(
            UnitPrice.Gross(123m),
            Quantity.One,
            VatRate.Of(23),
            Discount.OfAmount(10m));

        var result = engine.Calculate([item], VatCalculationMethod.FromSumOfGrossValues);

        var summary = result.VatRateSummaries.Single();
        summary.TotalGross.Value.Should().Be(113.00m);
        summary.TotalVat.Value.Should().Be(21.13m);      // 113 × 23/123 = 21.13008 → 21.13
        summary.TotalNet.Value.Should().Be(91.87m);       // 113 − 21.13
    }
}