using FluentAssertions;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Strategies.Rounding;
using Inflop.VatSharp.ValueObjects;
using Xunit;

namespace Inflop.VatSharp.Tests;

public class LineItemEngineFcyTests
{
    private record LineDto(decimal Price, int Qty, int Vat);

    private static readonly ExchangeRate EurPln = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.2345m, new DateOnly(2024, 10, 21), "NBP");

    [Fact]
    public void ForItems_FcyOverload_ProducesCorrectPlnVat()
    {
        var engine = VatCalculationEngine.ForItems<LineDto>(cfg => cfg
            .NetUnitPrice(p => p.Price)
            .Quantity(p => p.Qty)
            .VatRate(p => p.Vat));

        var lines = new[] { new LineDto(250m, 1, 23) };   // 250 EUR, 23%

        var result = engine.Calculate(lines, VatCalculationMethod.FromSumOfNetValues, EurPln);

        // VAT EUR: 250 × 23% = 57.50
        // NetBase = 250 × 4.2345 = 1058.63 → VatBase = 1058.63 × 23% = 243.48
        result.TotalVat.Value.Should().Be(57.50m);
        result.TotalVatBase.Value.Should().Be(243.48m);
    }

    [Fact]
    public void ForItems_WithBaseCurrencyRounding_AppliesCustomPrecisionToBaseAmounts()
    {
        // EUR invoice, JPY base (0dp) — verifies baseCurrencyRounding propagates through ForItems
        // Net 100 EUR @ 23%, rate 161.47 JPY/EUR
        // VatBase (0dp) = round(round(100 × 161.47, 0dp) × 0.23, 0dp)
        //               = round(16147 × 0.23, 0dp) = round(3713.81) = 3714
        var eurJpy = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.Of("JPY"), 161.47m);
        var engine = VatCalculationEngine.ForItems<LineDto>(
            cfg => cfg.NetUnitPrice(p => p.Price).Quantity(p => p.Qty).VatRate(p => p.Vat),
            baseCurrencyRounding: DefaultRounding.ZeroDecimalPlaces);

        var result = engine.Calculate(
            [new LineDto(100m, 1, 23)],
            VatCalculationMethod.FromSumOfNetValues,
            eurJpy);

        result.TotalVat.Value.Should().Be(23.00m);
        result.TotalVatBase.Value.Should().Be(3714m);
        result.TotalNetBase.Value.Should().Be(16147m);
    }

    [Fact]
    public void ForItems_MethodII_WithPercentageDiscount_FcyConvertsBaseFromGross()
    {
        // Retail scenario: 1 × 123.00 EUR brutto, 23%, 10% discount, NBP rate 4.2140
        //
        // EUR side (Method II — authoritative field: Gross):
        //   Gross before discount:  123.00
        //   Gross after discount:   123.00 × 0.90 = 110.70
        //   VATFromGross:           110.70 × 23/123 = 20.70
        //   Net:                    110.70 − 20.70 = 90.00
        //   Discount (net-equiv):   NetFromGross(123) − NetFromGross(110.70) = 100 − 90 = 10.00
        //
        // PLN base (GrossBase is authoritative for Method II):
        //   GrossBase = round(110.70 × 4.2140) = round(466.4898) = 466.49
        //   VatBase   = VatFromGross(466.49) = round(466.49 × 23/123) = round(87.2299) = 87.23
        //   NetBase   = 466.49 − 87.23 = 379.26
        //   DiscountBase = round(10.00 × 4.2140) = 42.14
        var eurPln = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.2140m,
            new DateOnly(2026, 2, 25), "NBP");

        var items = new[]
        {
            new InvoiceLineItem(
                UnitPrice.Gross(123m), Quantity.One, VatRate.Of(23), Discount.OfPercentage(10m)),
        };

        var result = VatCalculationEngine.Create()
            .Calculate(items, VatCalculationMethod.FromSumOfGrossValues, eurPln);

        var summary = result.VatRateSummaries.Single();

        // Invoice currency (EUR)
        summary.TotalGross.Value.Should().Be(110.70m);
        summary.TotalVat.Value.Should().Be(20.70m);
        summary.TotalNet.Value.Should().Be(90.00m);
        summary.TotalDiscount.Value.Should().Be(10.00m);

        // Base currency (PLN) — Method II derives base from Gross
        summary.TotalGrossBase.Value.Should().Be(466.49m);
        summary.TotalVatBase.Value.Should().Be(87.23m);
        summary.TotalNetBase.Value.Should().Be(379.26m);
        summary.TotalDiscountBase.Value.Should().Be(42.14m);
    }
}
