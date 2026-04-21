using FluentAssertions;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Exceptions;
using Inflop.VatSharp.Mapping;
using Inflop.VatSharp.Strategies.Rounding;
using Inflop.VatSharp.ValueObjects;
using Xunit;

namespace Inflop.VatSharp.Tests;

public class DirectEngineTests
{
    private static readonly InvoiceLineItem[] ArticleItems =
    [
        new(UnitPrice.Net(4.58m),  Quantity.Of(4), VatRate.Of(23)),
        new(UnitPrice.Net(7.22m),  Quantity.Of(5), VatRate.Of(23)),
        new(UnitPrice.Net(12.74m), Quantity.Of(2), VatRate.Of(8)),
    ];

    [Fact]
    public void Create_Default_CalculatesCorrectly()
    {
        var engine = VatCalculationEngine.Create();
        var result = engine.Calculate(ArticleItems, VatCalculationMethod.FromSumOfNetValues);

        result.TotalNet.Value.Should().Be(79.90m);
        result.TotalVat.Value.Should().Be(14.56m);
        result.TotalGross.Value.Should().Be(94.46m);
    }

    [Fact]
    public void Create_WithCustomRounding_AppliesRounding()
    {
        var engine = VatCalculationEngine.Create(rounding: DefaultRounding.ZeroDecimalPlaces);
        var item = new InvoiceLineItem(UnitPrice.Net(1000m), Quantity.One, VatRate.Of(27));

        var amounts = engine.CalculateLineItem(item);

        amounts.VatAmount.Value.Should().Be(270m);
    }

    [Fact]
    public void Create_CalculateLineItem_Works()
    {
        var engine = VatCalculationEngine.Create();
        var item = new InvoiceLineItem(UnitPrice.Net(6.23m), Quantity.Of(5), VatRate.Of(23));

        var amounts = engine.CalculateLineItem(item);

        amounts.NetValue.Value.Should().Be(31.15m);
        amounts.VatAmount.Value.Should().Be(7.16m);
        amounts.GrossValue.Value.Should().Be(38.31m);
    }

    [Fact]
    public void Create_FcyCalculation_ReturnsCorrectAmounts()
    {
        var engine = VatCalculationEngine.Create();
        var rate = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.2345m, new DateOnly(2024, 10, 21), "NBP");
        var item = new InvoiceLineItem(UnitPrice.Net(100m), Quantity.One, VatRate.Of(23));

        var result = engine.Calculate([item], VatCalculationMethod.FromSumOfNetValues, rate);

        result.TotalVat.Value.Should().Be(23m);
        result.TotalVatBase.Value.Should().Be(97.39m);
        result.Currency.Should().Be(CurrencyCode.EUR);
    }

    [Fact]
    public void Create_PlnCalculation_ReturnsDocumentAmounts()
    {
        var engine = VatCalculationEngine.Create();
        var item = new InvoiceLineItem(UnitPrice.Net(100m), Quantity.One, VatRate.Of(23));

        var result = engine.Calculate([item], VatCalculationMethod.FromSumOfNetValues);

        result.TotalVat.Value.Should().Be(23m);
        result.Should().BeOfType<DocumentAmounts>();
    }

    [Fact]
    public void Create_ReturnsCorrectEngineType()
    {
        var engine = VatCalculationEngine.Create();
        engine.Should().BeOfType<LineItemCalculationEngine<InvoiceLineItem>>();
    }

    [Fact]
    public void Create_FromSumOfGrossValues_CalculatesCorrectly()
    {
        var engine = VatCalculationEngine.Create();
        var grossItems = new InvoiceLineItem[]
        {
            new(UnitPrice.Gross(22.53m), Quantity.Of(1), VatRate.Of(23)),
            new(UnitPrice.Gross(44.40m), Quantity.Of(1), VatRate.Of(23)),
            new(UnitPrice.Gross(27.52m), Quantity.Of(1), VatRate.Of(8)),
        };

        var result = engine.Calculate(grossItems, VatCalculationMethod.FromSumOfGrossValues);

        var vat23 = result.VatRateSummaries.Single(s => s.VatRate == VatRate.Of(23));
        vat23.TotalGross.Value.Should().Be(66.93m);
        vat23.TotalVat.Value.Should().Be(12.52m);
    }

    [Fact]
    public void Create_SumOfLineItemVatAmounts_PennyDifference()
    {
        var engine = VatCalculationEngine.Create();
        var m1 = engine.Calculate(ArticleItems, VatCalculationMethod.FromSumOfNetValues);
        var m3 = engine.Calculate(ArticleItems, VatCalculationMethod.SumOfLineItemVatAmounts);

        (m1.TotalVat.Value - m3.TotalVat.Value).Should().Be(0.01m);
    }

    [Fact]
    public void Create_ZeroRate_NoVat()
    {
        var engine = VatCalculationEngine.Create();
        var items = new[] { new InvoiceLineItem(UnitPrice.Net(1000m), Quantity.One, VatRate.Zero) };

        var result = engine.Calculate(items, VatCalculationMethod.FromSumOfNetValues);

        result.TotalVat.Value.Should().Be(0m);
        result.TotalNet.Value.Should().Be(1000m);
    }

    [Fact]
    public void Create_EmptyLineItems_Throws()
    {
        var engine = VatCalculationEngine.Create();
        var act = () => engine.Calculate([], VatCalculationMethod.FromSumOfNetValues);
        act.Should().Throw<VatCalculationException>();
    }

    [Fact]
    public void Create_MultipleRates_SortedDescending()
    {
        var engine = VatCalculationEngine.Create();
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(100m), Quantity.One, VatRate.Of(5)),
            new(UnitPrice.Net(100m), Quantity.One, VatRate.Of(23)),
            new(UnitPrice.Net(100m), Quantity.One, VatRate.Of(8)),
        };

        var result = engine.Calculate(items, VatCalculationMethod.FromSumOfNetValues);

        result.VatRateSummaries.Select(s => s.VatRate.Percentage)
            .Should().BeInDescendingOrder();
    }

    [Fact]
    public void Create_CustomRounding_RoundingAffectsResult()
    {
        var item = new InvoiceLineItem(UnitPrice.Net(107.50m), Quantity.One, VatRate.Of(23));
        var standard  = VatCalculationEngine.Create().CalculateLineItem(item);
        var wholeUnit = VatCalculationEngine.Create(rounding: DefaultRounding.ZeroDecimalPlaces).CalculateLineItem(item);

        standard.VatAmount.Value.Should().Be(24.73m);
        wholeUnit.VatAmount.Value.Should().Be(25m);
    }

    [Fact]
    public void Create_FromSumOfGrossValues_WithNetUnitPrice_Throws()
    {
        var engine = VatCalculationEngine.Create();
        var items = new[] { new InvoiceLineItem(UnitPrice.Net(0.99m), Quantity.Of(17), VatRate.Of(23)) };

        var act = () => engine.Calculate(items, VatCalculationMethod.FromSumOfGrossValues);

        act.Should().Throw<VatCalculationException>().WithMessage("*UnitPrice.Gross()*");
    }

    [Fact]
    public void Create_FromSumOfGrossValues_WithMixedPrices_Throws()
    {
        var engine = VatCalculationEngine.Create();
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Gross(22.53m), Quantity.Of(1), VatRate.Of(23)),
            new(UnitPrice.Gross(44.40m), Quantity.Of(1), VatRate.Of(23)),
            new(UnitPrice.Net(0.99m),    Quantity.Of(1), VatRate.Of(23)),
        };

        var act = () => engine.Calculate(items, VatCalculationMethod.FromSumOfGrossValues);

        act.Should().Throw<VatCalculationException>().WithMessage("*UnitPrice.Gross()*");
    }

    [Fact]
    public void Create_SumOfLineItemVatAmounts_ArticleData()
    {
        var engine = VatCalculationEngine.Create();
        var result = engine.Calculate(ArticleItems, VatCalculationMethod.SumOfLineItemVatAmounts);

        result.TotalVat.Value.Should().Be(14.55m);
        result.LineItems[0].VatAmount.Value.Should().Be(4.21m);
        result.LineItems[1].VatAmount.Value.Should().Be(8.30m);
        result.LineItems[2].VatAmount.Value.Should().Be(2.04m);

        var vat23 = result.VatRateSummaries.Single(s => s.VatRate == VatRate.Of(23));
        vat23.TotalVat.Value.Should().Be(12.51m);
    }

    [Fact]
    public void Create_NullLineItems_Throws()
    {
        var engine = VatCalculationEngine.Create();
        var act = () => engine.Calculate(null!, VatCalculationMethod.FromSumOfNetValues);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_MixedPriceTypes_WorksCorrectly()
    {
        var engine = VatCalculationEngine.Create();
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(100m),   Quantity.One, VatRate.Of(23)),
            new(UnitPrice.Gross(123m), Quantity.One, VatRate.Of(23)),
        };

        var result = engine.Calculate(items, VatCalculationMethod.FromSumOfNetValues);

        result.TotalNet.Value.Should().Be(200m);
        result.TotalVat.Value.Should().Be(46m);
    }

    [Fact]
    public void Create_FractionalQuantity_Works()
    {
        var engine = VatCalculationEngine.Create();
        var item = new InvoiceLineItem(UnitPrice.Net(15.00m), Quantity.Of(2.5m), VatRate.Of(23));

        var amounts = engine.CalculateLineItem(item);

        amounts.NetValue.Value.Should().Be(37.50m);
        amounts.VatAmount.Value.Should().Be(8.63m);
    }

    [Fact]
    public void Create_ZeroRate_NoVat_IncludingGross()
    {
        var engine = VatCalculationEngine.Create();
        var items = new[] { new InvoiceLineItem(UnitPrice.Net(1000m), Quantity.One, VatRate.Zero) };

        var result = engine.Calculate(items, VatCalculationMethod.FromSumOfNetValues);

        result.TotalVat.Value.Should().Be(0m);
        result.TotalNet.Value.Should().Be(1000m);
        result.TotalGross.Value.Should().Be(1000m);
    }

    [Theory]
    [InlineData(VatCalculationMethod.FromSumOfNetValues)]
    [InlineData(VatCalculationMethod.FromSumOfGrossValues)]
    [InlineData(VatCalculationMethod.SumOfLineItemVatAmounts)]
    public void Calculate_InterleavedVatRates_LineItemsPreserveInputOrder(VatCalculationMethod method)
    {
        var engine = VatCalculationEngine.Create();
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(100m),  Quantity.One, VatRate.Of(8)),
            new(UnitPrice.Net(200m),  Quantity.One, VatRate.Of(23)),
            new(UnitPrice.Net(300m),  Quantity.One, VatRate.Of(8)),
        };

        if (method == VatCalculationMethod.FromSumOfGrossValues)
        {
            items =
            [
                new(UnitPrice.Gross(108m), Quantity.One, VatRate.Of(8)),
                new(UnitPrice.Gross(246m), Quantity.One, VatRate.Of(23)),
                new(UnitPrice.Gross(324m), Quantity.One, VatRate.Of(8)),
            ];
        }

        var result = engine.Calculate(items, method);

        result.LineItems[0].VatRate.Should().Be(VatRate.Of(8));
        result.LineItems[1].VatRate.Should().Be(VatRate.Of(23));
        result.LineItems[2].VatRate.Should().Be(VatRate.Of(8));
    }

    // ── Per-rate summary breakdown ──────────────────────────────────────────

    [Fact]
    public void Create_FromSumOfNetValues_PerRateSummaryBreakdown()
    {
        var engine = VatCalculationEngine.Create();
        var result = engine.Calculate(ArticleItems, VatCalculationMethod.FromSumOfNetValues);

        result.VatRateSummaries.Should().HaveCount(2);

        var vat23 = result.VatRateSummaries.First(s => s.VatRate == VatRate.Of(23));
        vat23.TotalNet.Value.Should().Be(54.42m);      // 4×4.58 + 5×7.22 = 18.32 + 36.10
        vat23.TotalVat.Value.Should().Be(12.52m);      // 54.42 × 23% = 12.5166 → 12.52
        vat23.TotalGross.Value.Should().Be(66.94m);    // 54.42 + 12.52

        var vat8 = result.VatRateSummaries.First(s => s.VatRate == VatRate.Of(8));
        vat8.TotalNet.Value.Should().Be(25.48m);       // 2×12.74
        vat8.TotalVat.Value.Should().Be(2.04m);        // 25.48 × 8% = 2.0384 → 2.04
        vat8.TotalGross.Value.Should().Be(27.52m);     // 25.48 + 2.04
    }

    [Fact]
    public void Create_SumOfLineItemVatAmounts_PerRateSummaryBreakdown()
    {
        var engine = VatCalculationEngine.Create();
        var result = engine.Calculate(ArticleItems, VatCalculationMethod.SumOfLineItemVatAmounts);

        result.VatRateSummaries.Should().HaveCount(2);

        var vat23 = result.VatRateSummaries.First(s => s.VatRate == VatRate.Of(23));
        vat23.TotalNet.Value.Should().Be(54.42m);
        vat23.TotalVat.Value.Should().Be(12.51m);      // 4.21 + 8.30 (per-line rounded)
        vat23.TotalGross.Value.Should().Be(66.93m);    // 54.42 + 12.51

        var vat8 = result.VatRateSummaries.First(s => s.VatRate == VatRate.Of(8));
        vat8.TotalNet.Value.Should().Be(25.48m);
        vat8.TotalVat.Value.Should().Be(2.04m);
        vat8.TotalGross.Value.Should().Be(27.52m);
    }

    // ── Method II multi-rate gross ──────────────────────────────────────────

    [Fact]
    public void Create_FromSumOfGrossValues_MultiRate_CalculatesCorrectly()
    {
        var engine = VatCalculationEngine.Create();
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Gross(123.00m), Quantity.One, VatRate.Of(23)),  // Gross 123.00
            new(UnitPrice.Gross(108.00m), Quantity.One, VatRate.Of(8)),   // Gross 108.00
        };

        var result = engine.Calculate(items, VatCalculationMethod.FromSumOfGrossValues);

        var vat23 = result.VatRateSummaries.First(s => s.VatRate == VatRate.Of(23));
        vat23.TotalGross.Value.Should().Be(123.00m);
        vat23.TotalVat.Value.Should().Be(23.00m);     // 123 × 23/123 = 23.00
        vat23.TotalNet.Value.Should().Be(100.00m);    // 123 − 23 = 100

        var vat8 = result.VatRateSummaries.First(s => s.VatRate == VatRate.Of(8));
        vat8.TotalGross.Value.Should().Be(108.00m);
        vat8.TotalVat.Value.Should().Be(8.00m);       // 108 × 8/108 = 8.00
        vat8.TotalNet.Value.Should().Be(100.00m);     // 108 − 8 = 100

        result.TotalNet.Value.Should().Be(200.00m);
        result.TotalVat.Value.Should().Be(31.00m);
        result.TotalGross.Value.Should().Be(231.00m);
    }

    // ── Zero-rate with Method II and Method III ────────────────────────────

    [Fact]
    public void Create_FromSumOfGrossValues_ZeroRate_NoVat()
    {
        var engine = VatCalculationEngine.Create();
        var items = new[] { new InvoiceLineItem(UnitPrice.Gross(1000m), Quantity.One, VatRate.Zero) };

        var result = engine.Calculate(items, VatCalculationMethod.FromSumOfGrossValues);

        result.TotalVat.Value.Should().Be(0m);
        result.TotalNet.Value.Should().Be(1000m);
        result.TotalGross.Value.Should().Be(1000m);
    }

    [Fact]
    public void Create_SumOfLineItemVatAmounts_ZeroRate_NoVat()
    {
        var engine = VatCalculationEngine.Create();
        var items = new[] { new InvoiceLineItem(UnitPrice.Net(1000m), Quantity.One, VatRate.Zero) };

        var result = engine.Calculate(items, VatCalculationMethod.SumOfLineItemVatAmounts);

        result.TotalVat.Value.Should().Be(0m);
        result.TotalNet.Value.Should().Be(1000m);
        result.TotalGross.Value.Should().Be(1000m);
    }

    // ── Small amounts ───────────────────────────────────────────────────────

    [Fact]
    public void Create_SmallAmount_01_CalculatesCorrectly()
    {
        // 100 szt. × 0.01 netto, 23%
        //   Net = 1.00, VAT = 0.23, Gross = 1.23
        var engine = VatCalculationEngine.Create();
        var items = new[] { new InvoiceLineItem(UnitPrice.Net(0.01m), Quantity.Of(100), VatRate.Of(23)) };

        var result = engine.Calculate(items, VatCalculationMethod.FromSumOfNetValues);

        result.TotalNet.Value.Should().Be(1.00m);
        result.TotalVat.Value.Should().Be(0.23m);
        result.TotalGross.Value.Should().Be(1.23m);
    }

    [Fact]
    public void Create_SmallAmount_SingleItem01_CalculatesCorrectly()
    {
        // 1 szt. × 0.01 netto, 23%
        //   Net = 0.01, VAT = 0.01 × 0.23 = 0.0023 → 0.00 (rounded to 2dp)
        var engine = VatCalculationEngine.Create();
        var item = new InvoiceLineItem(UnitPrice.Net(0.01m), Quantity.One, VatRate.Of(23));

        var amounts = engine.CalculateLineItem(item);

        amounts.NetValue.Value.Should().Be(0.01m);
        amounts.VatAmount.Value.Should().Be(0.00m);
        amounts.GrossValue.Value.Should().Be(0.01m);
    }

    // ── Method III domestic multi-rate ──────────────────────────────────────

    [Fact]
    public void Create_SumOfLineItemVatAmounts_MultiRate_CalculatesCorrectly()
    {
        var engine = VatCalculationEngine.Create();
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(100m), Quantity.One, VatRate.Of(23)),
            new(UnitPrice.Net(100m), Quantity.One, VatRate.Of(8)),
        };

        var result = engine.Calculate(items, VatCalculationMethod.SumOfLineItemVatAmounts);

        result.LineItems[0].VatAmount.Value.Should().Be(23.00m);   // 100 × 23% = 23.00
        result.LineItems[1].VatAmount.Value.Should().Be(8.00m);    // 100 × 8% = 8.00

        var vat23 = result.VatRateSummaries.First(s => s.VatRate == VatRate.Of(23));
        vat23.TotalVat.Value.Should().Be(23.00m);

        var vat8 = result.VatRateSummaries.First(s => s.VatRate == VatRate.Of(8));
        vat8.TotalVat.Value.Should().Be(8.00m);

        result.TotalNet.Value.Should().Be(200m);
        result.TotalVat.Value.Should().Be(31.00m);
        result.TotalGross.Value.Should().Be(231.00m);
    }
}
