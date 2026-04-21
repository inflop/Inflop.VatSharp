using FluentAssertions;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Mapping;
using Inflop.VatSharp.ValueObjects;
using Inflop.VatSharp.Exceptions;
using Inflop.VatSharp.Strategies.Rounding;
using Xunit;

namespace Inflop.VatSharp.Tests;

public class ForeignCurrencyCalculatorTests
{
    private static readonly DateOnly RateDate = new(2024, 10, 21);
    // 1 EUR = 4.2345 PLN (NBP)
    private static readonly ExchangeRate EurPln = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.2345m, RateDate, "NBP");
    private readonly ForeignCurrencyCalculator _calc = new();

    [Fact]
    public void MethodI_MultiRate_CorrectFcyAndBaseAmounts()
    {
        // Software 1000 EUR + Support 250 EUR @ 23%
        // Hardware 500 EUR @ 8%
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(1000m), Quantity.One, VatRate.Of(23)),
            new(UnitPrice.Net(250m),  Quantity.One, VatRate.Of(23)),
            new(UnitPrice.Net(500m),  Quantity.One, VatRate.Of(8)),
        };

        var result = _calc.Calculate(items, VatCalculationMethod.FromSumOfNetValues, EurPln);

        // FCY totals
        result.TotalNet.Value.Should().Be(1750m);
        result.TotalVat.Value.Should().Be(327.50m);
        result.TotalGross.Value.Should().Be(2077.50m);
        result.Currency.Should().Be(CurrencyCode.EUR);

        // Per-rate FCY + base (PLN)
        var vat23 = result.VatRateSummaries.Single(s => s.VatRate == VatRate.Of(23));
        vat23.TotalNet.Value.Should().Be(1250m);
        vat23.TotalVat.Value.Should().Be(287.50m);
        vat23.TotalVatBase.Value.Should().Be(1217.42m);   // NetBase = 1250 × 4.2345 = 5293.13 → VatBase = 5293.13 × 23% = 1217.42

        var vat8 = result.VatRateSummaries.Single(s => s.VatRate == VatRate.Of(8));
        vat8.TotalVat.Value.Should().Be(40m);
        vat8.TotalVatBase.Value.Should().Be(169.38m);     // NetBase = 500 × 4.2345 = 2117.25 → VatBase = 2117.25 × 8% = 169.38

        result.TotalVatBase.Value.Should().Be(1386.80m);  // 1217.42 + 169.38
    }

    [Fact]
    public void MethodII_GrossPrices_CorrectVatAndBaseVat()
    {
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Gross(123m), Quantity.One, VatRate.Of(23)),
            new(UnitPrice.Gross(54m),  Quantity.One, VatRate.Of(8)),
        };

        var result = _calc.Calculate(items, VatCalculationMethod.FromSumOfGrossValues, EurPln);

        var vat23 = result.VatRateSummaries.Single(s => s.VatRate == VatRate.Of(23));
        vat23.TotalGross.Value.Should().Be(123m);
        vat23.TotalVat.Value.Should().Be(23m);
        vat23.TotalVatBase.Value.Should().Be(97.39m);   // GrossBase = 123 × 4.2345 = 520.84 → VatBase = VatFromGross(520.84) = 97.39

        var vat8 = result.VatRateSummaries.Single(s => s.VatRate == VatRate.Of(8));
        vat8.TotalGross.Value.Should().Be(54m);
        vat8.TotalVat.Value.Should().Be(4m);
        vat8.TotalVatBase.Value.Should().Be(16.94m);    // GrossBase = 54 × 4.2345 = 228.66 → VatBase = VatFromGross(228.66) = 16.94
    }

    [Fact]
    public void WithDiscount_DiscountReducesFcyVatAndBaseVat()
    {
        // 200 EUR netto, 23%, rabat 10% → net 180, VAT 41.40, VAT_base 175.31
        var item = new InvoiceLineItem(UnitPrice.Net(200m), Quantity.One, VatRate.Of(23), Discount.OfPercentage(10m));

        var result = _calc.Calculate([item], VatCalculationMethod.FromSumOfNetValues, EurPln);

        result.TotalNet.Value.Should().Be(180m);
        result.TotalVat.Value.Should().Be(41.40m);
        result.TotalVatBase.Value.Should().Be(175.31m);   // NetBase = 180 × 4.2345 = 762.21 → VatBase = 762.21 × 23% = 175.31
        result.TotalDiscount.Value.Should().Be(20m);
    }

    [Fact]
    public void MethodII_WithDiscount_DiscountReducesFcyVatAndBaseVat()
    {
        // 123 EUR gross @ 23%, 10% discount
        //   gross line total = 123 − (10% × 123) = 110.70
        //   TotalVat = VatFromGross(110.70, 23%) = round(110.70 × 23/123) = round(20.699918) = 20.70
        //   TotalNet = 110.70 − 20.70 = 90.00
        //   TotalDiscount (net) = TotalNetBeforeDiscount − TotalNet = 100.00 − 90.00 = 10.00
        //   GrossBase    = round(110.70 × 4.2345) = round(468.75915) = 468.76
        //   VatBase      = VatFromGross(468.76, 23%) = round(468.76 × 23/123) = round(87.6543) = 87.65
        //   NetBase      = 468.76 − 87.65 = 381.11
        //   DiscountBase = round(10.00 × 4.2345) = round(42.345) = 42.35
        var item = new InvoiceLineItem(UnitPrice.Gross(123m), Quantity.One, VatRate.Of(23), Discount.OfPercentage(10m));

        var result = _calc.Calculate([item], VatCalculationMethod.FromSumOfGrossValues, EurPln);

        result.TotalGross.Value.Should().Be(110.70m);
        result.TotalVat.Value.Should().Be(20.70m);
        result.TotalNet.Value.Should().Be(90.00m);
        result.TotalDiscount.Value.Should().Be(10.00m);

        result.TotalGrossBase.Value.Should().Be(468.76m);
        result.TotalVatBase.Value.Should().Be(87.65m);
        result.TotalNetBase.Value.Should().Be(381.11m);
        result.TotalDiscountBase.Value.Should().Be(42.35m);
    }

    [Fact]
    public void EcbSource_PreservedInResult()
    {
        var ecbRate = ExchangeRate.Of(CurrencyCode.USD, CurrencyCode.PLN, 3.98m, new DateOnly(2024, 6, 15), "ECB");
        var item = new InvoiceLineItem(UnitPrice.Net(100m), Quantity.One, VatRate.Of(23));

        var result = _calc.Calculate([item], VatCalculationMethod.FromSumOfNetValues, ecbRate);

        result.ExchangeRate.Source.Should().Be("ECB");
        result.ExchangeRate.RateDate.Should().Be(new DateOnly(2024, 6, 15));
    }

    [Fact]
    public void BaseVat_DefaultsToTwoDecimalPlaces_EvenWhenFcyRoundingIs0dp()
    {
        // Use 0dp rounding for FCY amounts (e.g. HUF-like scenario)
        var calcWith0dp = new ForeignCurrencyCalculator(DefaultRounding.ZeroDecimalPlaces);
        var item = new InvoiceLineItem(UnitPrice.Net(100m), Quantity.One, VatRate.Of(23));

        var result = calcWith0dp.Calculate([item], VatCalculationMethod.FromSumOfNetValues, EurPln);

        // NetBase = 100 × 4.2345 = 423.45 → VatBase = 423.45 × 23% = 97.39 (always 2dp)
        result.TotalVatBase.Value.Should().Be(97.39m);
    }

    [Fact]
    public void Calculate_EmptyItems_Throws()
        => FluentActions.Invoking(() => _calc.Calculate([], VatCalculationMethod.FromSumOfNetValues, EurPln))
            .Should()
            .Throw<VatCalculationException>();

    [Fact]
    public void ToBaseDocumentAmounts_BaseVatBecomesVat()
    {
        var item = new InvoiceLineItem(UnitPrice.Net(1000m), Quantity.One, VatRate.Of(23));
        var result = _calc.Calculate([item], VatCalculationMethod.FromSumOfNetValues, EurPln);

        var baseAmounts = result.ToBaseDocumentAmounts();

        baseAmounts.TotalVat.Value.Should().Be(result.TotalVatBase.Value);
        baseAmounts.Method.Should().Be(result.Method);
    }

    [Fact]
    public void VatRateSummaryFcy_ToBaseSummary_UsesAllBaseAmounts()
    {
        var item = new InvoiceLineItem(UnitPrice.Net(100m), Quantity.One, VatRate.Of(23));
        var result = _calc.Calculate([item], VatCalculationMethod.FromSumOfNetValues, EurPln);

        var fcySummary = result.VatRateSummaries.Single();
        var baseSummary = fcySummary.ToBaseSummary();

        baseSummary.TotalNet.Should().Be(fcySummary.TotalNetBase);
        baseSummary.TotalVat.Should().Be(fcySummary.TotalVatBase);
        baseSummary.TotalGross.Should().Be(fcySummary.TotalGrossBase);
        baseSummary.TotalDiscount.Should().Be(fcySummary.TotalDiscountBase);
    }

    [Fact]
    public void NonPln_UsdToEur_CalculatesCorrectBaseVat()
    {
        // USD invoice, EUR base — e.g. for EU-registered entity invoicing in USD
        var usdEurRate = ExchangeRate.Of(
            CurrencyCode.USD, CurrencyCode.EUR, 0.92m,
            RateDate, "ECB");

        var item = new InvoiceLineItem(UnitPrice.Net(100m), Quantity.One, VatRate.Of(20));

        var result = _calc.Calculate([item], VatCalculationMethod.FromSumOfNetValues, usdEurRate);

        result.TotalVat.Value.Should().Be(20m);              // USD
        result.TotalVatBase.Value.Should().Be(18.40m);       // NetBase = 100 × 0.92 = 92.00 → VatBase = 92.00 × 20% = 18.40 EUR
        result.ExchangeRate.BaseCurrency.Should().Be(CurrencyCode.EUR);
    }

    // ── Method III (SumOfLineItemVatAmounts) FCY ─────────────────────────────

    [Fact]
    public void MethodIII_SingleRate_ConvertsBothNetAndVatIndependently()
    {
        // 3 × 10.33 EUR net @ 23%
        // Method III per-line VAT: round(10.33 × 0.23) = 2.38 each → total VAT = 7.14
        // Method I would give: round(30.99 × 0.23) = 7.13 — penny difference
        // FCY conversion: NetBase and VatBase are converted independently (not derived from each other)
        //   NetBase = round(30.99 × 4.2345) = 131.23
        //   VatBase = round(7.14 × 4.2345) = 30.23   ← Method I would give round(131.23 × 0.23) = 30.18
        //   GrossBase = 131.23 + 30.23 = 161.46
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(10.33m), Quantity.One, VatRate.Of(23)),
            new(UnitPrice.Net(10.33m), Quantity.One, VatRate.Of(23)),
            new(UnitPrice.Net(10.33m), Quantity.One, VatRate.Of(23)),
        };

        var result = _calc.Calculate(items, VatCalculationMethod.SumOfLineItemVatAmounts, EurPln);

        result.TotalNet.Value.Should().Be(30.99m);
        result.TotalVat.Value.Should().Be(7.14m);
        result.TotalGross.Value.Should().Be(38.13m);

        var summary = result.VatRateSummaries.Single();
        summary.TotalNetBase.Value.Should().Be(131.23m);
        summary.TotalVatBase.Value.Should().Be(30.23m);
        summary.TotalGrossBase.Value.Should().Be(161.46m);

        result.TotalNetBase.Value.Should().Be(131.23m);
        result.TotalVatBase.Value.Should().Be(30.23m);
        result.TotalGrossBase.Value.Should().Be(161.46m);
    }

    [Fact]
    public void MethodIII_MultiRate_EachRateSumConvertedIndependently()
    {
        // 1 × 100 EUR net @ 23%: VAT = round(100 × 0.23) = 23.00
        // 1 × 27.33 EUR net @ 8%: VAT = round(27.33 × 0.08) = 2.19
        //
        // FCY at 4.2345:
        //   23%: NetBase = round(100 × 4.2345) = 423.45
        //        VatBase = round(23.00 × 4.2345) = round(97.3935) = 97.39
        //        GrossBase = 423.45 + 97.39 = 520.84
        //   8%:  NetBase = round(27.33 × 4.2345) = round(115.728885) = 115.73
        //        VatBase = round(2.19 × 4.2345) = round(9.273555) = 9.27
        //              (Method I would give round(115.73 × 0.08) = round(9.2584) = 9.26)
        //        GrossBase = 115.73 + 9.27 = 125.00
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(100m),   Quantity.One, VatRate.Of(23)),
            new(UnitPrice.Net(27.33m), Quantity.One, VatRate.Of(8)),
        };

        var result = _calc.Calculate(items, VatCalculationMethod.SumOfLineItemVatAmounts, EurPln);

        var vat23 = result.VatRateSummaries.Single(s => s.VatRate == VatRate.Of(23));
        vat23.TotalNet.Value.Should().Be(100m);
        vat23.TotalVat.Value.Should().Be(23m);
        vat23.TotalNetBase.Value.Should().Be(423.45m);
        vat23.TotalVatBase.Value.Should().Be(97.39m);
        vat23.TotalGrossBase.Value.Should().Be(520.84m);

        var vat8 = result.VatRateSummaries.Single(s => s.VatRate == VatRate.Of(8));
        vat8.TotalNet.Value.Should().Be(27.33m);
        vat8.TotalVat.Value.Should().Be(2.19m);
        vat8.TotalNetBase.Value.Should().Be(115.73m);
        vat8.TotalVatBase.Value.Should().Be(9.27m);
        vat8.TotalGrossBase.Value.Should().Be(125.00m);

        result.TotalNetBase.Value.Should().Be(539.18m);
        result.TotalVatBase.Value.Should().Be(106.66m);
        result.TotalGrossBase.Value.Should().Be(645.84m);
    }

    [Fact]
    public void MethodIII_WithDiscount_DiscountBaseConvertedIndependently()
    {
        // 200 EUR net @ 23%, 10% discount → net 180, VAT 41.40, discount 20
        //   NetBase   = round(180 × 4.2345) = round(762.21) = 762.21
        //   VatBase   = round(41.40 × 4.2345) = round(175.3083) = 175.31
        //   GrossBase = 762.21 + 175.31 = 937.52
        //   DiscountBase = round(20 × 4.2345) = round(84.69) = 84.69
        var item = new InvoiceLineItem(UnitPrice.Net(200m), Quantity.One, VatRate.Of(23), Discount.OfPercentage(10m));

        var result = _calc.Calculate([item], VatCalculationMethod.SumOfLineItemVatAmounts, EurPln);

        result.TotalNet.Value.Should().Be(180m);
        result.TotalVat.Value.Should().Be(41.40m);
        result.TotalDiscount.Value.Should().Be(20m);

        result.TotalNetBase.Value.Should().Be(762.21m);
        result.TotalVatBase.Value.Should().Be(175.31m);
        result.TotalGrossBase.Value.Should().Be(937.52m);
        result.TotalDiscountBase.Value.Should().Be(84.69m);
    }

    // ── BaseCurrencyRounding ─────────────────────────────────────────────────

    [Fact]
    public void BaseCurrencyRounding_ZeroDecimalPlaces_BaseAmountsRoundedToWholeUnits()
    {
        // EUR invoice, JPY base (0 decimal places — no fractional currency unit)
        // 1 EUR = 161.47 JPY
        // Net 100 EUR @ 23% → NetBase = round(100 × 161.47, 0dp) = 16147
        //                       VatBase = round(16147 × 0.23, 0dp) = round(3713.81) = 3714
        //                       GrossBase = 16147 + 3714 = 19861
        // Default 2dp would give: VatBase = 3713.81 — not a valid JPY amount
        var eurJpy = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.Of("JPY"), 161.47m);
        var calc = new ForeignCurrencyCalculator(baseCurrencyRounding: DefaultRounding.ZeroDecimalPlaces);
        var item = new InvoiceLineItem(UnitPrice.Net(100m), Quantity.One, VatRate.Of(23));

        var result = calc.Calculate([item], VatCalculationMethod.FromSumOfNetValues, eurJpy);

        result.TotalVat.Value.Should().Be(23.00m);        // EUR: unchanged
        result.TotalNetBase.Value.Should().Be(16147m);
        result.TotalVatBase.Value.Should().Be(3714m);
        result.TotalGrossBase.Value.Should().Be(19861m);
    }

    [Fact]
    public void BaseCurrencyRounding_ThreeDecimalPlaces_BaseAmountsRoundedTo3dp()
    {
        // EUR invoice, KWD base (3 decimal places — Kuwaiti dinar has fils)
        // 1 EUR = 0.3342 KWD
        // Net 100 EUR @ 23% → NetBase = round(100 × 0.3342, 3dp) = 33.420
        //                       VatBase = round(33.420 × 0.23, 3dp) = round(7.6866) = 7.687
        //                       GrossBase = 33.420 + 7.687 = 41.107
        // Default 2dp would give: VatBase = 7.69 — loses the sub-fil precision
        var eurKwd = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.Of("KWD"), 0.3342m);
        var calc = new ForeignCurrencyCalculator(baseCurrencyRounding: new DefaultRounding(3));
        var item = new InvoiceLineItem(UnitPrice.Net(100m), Quantity.One, VatRate.Of(23));

        var result = calc.Calculate([item], VatCalculationMethod.FromSumOfNetValues, eurKwd);

        result.TotalVat.Value.Should().Be(23.00m);
        result.TotalNetBase.Value.Should().Be(33.420m);
        result.TotalVatBase.Value.Should().Be(7.687m);
        result.TotalGrossBase.Value.Should().Be(41.107m);
    }

    [Fact]
    public void BaseCurrencyRounding_IndependentFromFcyRounding_EachAppliesOwnPrecision()
    {
        // FCY rounding = 0dp (HUF-like invoice), baseCurrencyRounding = 3dp
        // EUR → PLN 4.2345, Net 100 EUR @ 23%
        // FCY (0dp): VAT = round(100 × 0.23, 0dp) = 23
        // Base (3dp): NetBase = round(100 × 4.2345, 3dp) = 423.450
        //             VatBase = round(423.450 × 0.23, 3dp) = round(97.3935) = 97.394
        var calc = new ForeignCurrencyCalculator(
            rounding: DefaultRounding.ZeroDecimalPlaces,
            baseCurrencyRounding: new DefaultRounding(3));
        var item = new InvoiceLineItem(UnitPrice.Net(100m), Quantity.One, VatRate.Of(23));

        var result = calc.Calculate([item], VatCalculationMethod.FromSumOfNetValues, EurPln);

        result.TotalVat.Value.Should().Be(23m);           // 0dp FCY
        result.TotalVatBase.Value.Should().Be(97.394m);   // 3dp base
    }

    [Fact]
    public void MethodIII_Fcy_ProducesDifferentVatBaseThanMethodI_ForMultipleItemsSameRate()
    {
        // This test documents the legally correct difference between Method III and Method I FCY:
        // Method III converts the already-rounded per-line sum of VAT amounts,
        // while Method I re-derives VAT from the converted net.
        // Both results are valid — the methods are distinct legal options per Directive 2006/112/EC.
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(10.33m), Quantity.One, VatRate.Of(23)),
            new(UnitPrice.Net(10.33m), Quantity.One, VatRate.Of(23)),
            new(UnitPrice.Net(10.33m), Quantity.One, VatRate.Of(23)),
        };

        var resultIII = _calc.Calculate(items, VatCalculationMethod.SumOfLineItemVatAmounts, EurPln);
        var resultI   = _calc.Calculate(items, VatCalculationMethod.FromSumOfNetValues, EurPln);

        resultIII.TotalVat.Value.Should().Be(7.14m);       // per-line rounded: 3 × 2.38
        resultI.TotalVat.Value.Should().Be(7.13m);         // from sum of net: round(30.99 × 0.23)

        resultIII.TotalVatBase.Value.Should().Be(30.23m);  // round(7.14 × 4.2345)
        resultI.TotalVatBase.Value.Should().Be(30.18m);    // round(131.23 × 0.23) — derived from NetBase
    }
}
