using FluentAssertions;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Exceptions;
using Inflop.VatSharp.Mapping;
using Inflop.VatSharp.ValueObjects;
using Xunit;

namespace Inflop.VatSharp.Tests;

public class DocumentEngineFcyTests
{
    // ── Test fixtures ─────────────────────────────────────────────────────

    private record InvoiceLine(decimal Price, int Qty, int Vat);

    private record Invoice(
        InvoiceLine[] Lines,
        VatCalculationMethod CalcMethod,
        ExchangeRate ExRate);

    private static VatCalculationEngine<Invoice, InvoiceLine> BuildEngine()
        => VatCalculationEngine.For<Invoice, InvoiceLine>(cfg => cfg
            .Document(doc => doc
                .LineItems(f => f.Lines)
                .Method(f => f.CalcMethod)
                .ForeignCurrency(f => f.ExRate))
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .Quantity(p => p.Qty)
                .VatRate(p => p.Vat)));

    private static readonly ExchangeRate EurPln = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.2345m, new DateOnly(2025, 3, 1), "NBP");

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public void CalculateFcy_WithMappedExchangeRate_ReturnsCorrectAmounts()
    {
        var engine = BuildEngine();
        var invoice = new Invoice(
            Lines: [new InvoiceLine(250m, 1, 23)],   // 250 EUR, 23%
            CalcMethod: VatCalculationMethod.FromSumOfNetValues,
            ExRate: EurPln);

        var result = engine.CalculateFcy(invoice);

        // VAT EUR: 250 × 23% = 57.50
        // NetBase = 250 × 4.2345 = 1058.63 → VatBase = 1058.63 × 23% = 243.48
        result.Currency.Should().Be(CurrencyCode.EUR);
        result.TotalNet.Value.Should().Be(250.00m);
        result.TotalVat.Value.Should().Be(57.50m);
        result.TotalVatBase.Value.Should().Be(243.48m);
        result.TotalGross.Value.Should().Be(307.50m);
        result.ExchangeRate.Should().Be(EurPln);
    }

    [Fact]
    public void CalculateFcy_NonPlnBase_UsdToEur_ReturnsCorrectAmounts()
    {
        // German entity invoicing in USD, settling in EUR — 1 USD = 0.92 EUR
        var usdEur = ExchangeRate.Of(
            CurrencyCode.USD, CurrencyCode.EUR, 0.92m,
            new DateOnly(2025, 3, 1), "ECB");

        var engine = BuildEngine();
        var invoice = new Invoice(
            Lines: [new InvoiceLine(100m, 1, 19)],   // 100 USD, 19%
            CalcMethod: VatCalculationMethod.FromSumOfNetValues,
            ExRate: usdEur);

        var result = engine.CalculateFcy(invoice);

        // VAT USD: 100 × 19% = 19.00
        // NetBase = 100 × 0.92 = 92.00 → VatBase = 92.00 × 19% = 17.48
        result.Currency.Should().Be(CurrencyCode.USD);
        result.TotalVat.Value.Should().Be(19.00m);
        result.TotalVatBase.Value.Should().Be(17.48m);
    }

    // ── CalculateFcy not configured ───────────────────────────────────────

    [Fact]
    public void CalculateFcy_NotConfigured_ThrowsMappingConfigurationException()
    {
        var engine = VatCalculationEngine.For<Invoice, InvoiceLine>(cfg => cfg
            .Document(doc => doc
                .LineItems(f => f.Lines)
                .Method(f => f.CalcMethod))   // no ForeignCurrency
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .Quantity(p => p.Qty)
                .VatRate(p => p.Vat)));

        var invoice = new Invoice(
            Lines: [new InvoiceLine(100m, 1, 23)],
            CalcMethod: VatCalculationMethod.FromSumOfNetValues,
            ExRate: EurPln);

        var act = () => engine.CalculateFcy(invoice);

        act.Should().Throw<MappingConfigurationException>()
            .WithMessage("*ForeignCurrency*");
    }

    // ── Domestic Calculate still works when FCY mapping is present ────────

    [Fact]
    public void Calculate_StillWorksAfterAddingFcyMapping()
    {
        var engine = BuildEngine();
        var invoice = new Invoice(
            Lines: [new InvoiceLine(1000m, 1, 23), new InvoiceLine(500m, 1, 8)],
            CalcMethod: VatCalculationMethod.FromSumOfNetValues,
            ExRate: EurPln);

        var result = engine.Calculate(invoice);

        result.TotalNet.Value.Should().Be(1500.00m);
        result.TotalVat.Value.Should().Be(270.00m);   // 1000×23% + 500×8%
        result.TotalGross.Value.Should().Be(1770.00m);
    }

    // ── Constant rate (batch processing) ─────────────────────────────────

    [Fact]
    public void CalculateFcy_WithConstantExchangeRate_TDocNeedNoExchangeRateProperty()
    {
        // BatchInvoice has no ExchangeRate property — it is a pure domain type.
        // The rate is configured once in the engine, not stored per document.
        var batchEngine = VatCalculationEngine.For<BatchInvoice, InvoiceLine>(cfg => cfg
            .Document(doc => doc
                .LineItems(f => f.Lines)
                .Method(VatCalculationMethod.FromSumOfNetValues)
                .ForeignCurrency(EurPln))   // constant — no lambda, no ExchangeRate in TDoc
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .Quantity(p => p.Qty)
                .VatRate(p => p.Vat)));

        var invoice = new BatchInvoice(Lines: [new InvoiceLine(250m, 1, 23)]);

        var result = batchEngine.CalculateFcy(invoice);

        // VAT EUR: 250 × 23% = 57.50
        // NetBase = 250 × 4.2345 = 1058.63 → VatBase = 1058.63 × 23% = 243.48
        result.Currency.Should().Be(CurrencyCode.EUR);
        result.TotalVat.Value.Should().Be(57.50m);
        result.TotalVatBase.Value.Should().Be(243.48m);
        result.ExchangeRate.Should().Be(EurPln);
    }

    // TDoc without any VatSharp types — pure domain record:
    private record BatchInvoice(InvoiceLine[] Lines);

    // ── Method override (Calculate with methodOverride + exchangeRate) ────

    [Fact]
    public void Calculate_WithMethodOverride_UsesOverrideInsteadOfMappedMethod()
    {
        // Document is configured with FromSumOfNetValues; caller overrides to
        // SumOfLineItemVatAmounts. The penny-difference (0.01) proves the override
        // took effect — with 3 items and per-line rounding, Method III yields 14.55
        // while Method I yields 14.56.
        var engine = BuildEngine();
        var invoice = new Invoice(
            Lines:
            [
                new(4.58m, 4, 23),   // 4 × 4.58 = 18.32 → per-line VAT = 4.21
                new(7.22m, 5, 23),   // 5 × 7.22 = 36.10 → per-line VAT = 8.30
                new(12.74m, 2,  8),  // 2 × 12.74 = 25.48 → per-line VAT = 2.04
            ],
            CalcMethod: VatCalculationMethod.FromSumOfNetValues,   // document says Method I
            ExRate: EurPln);

        var resultI   = engine.CalculateFcy(invoice);   // uses mapped Method I
        var resultIII = engine.Calculate(              // override to Method III
            invoice,
            VatCalculationMethod.SumOfLineItemVatAmounts,
            EurPln);

        resultI.TotalVat.Value.Should().Be(14.56m);    // Method I
        resultIII.TotalVat.Value.Should().Be(14.55m);  // Method III — override applied
        resultIII.Method.Should().Be(VatCalculationMethod.SumOfLineItemVatAmounts);
    }

    // ── Null guard ────────────────────────────────────────────────────────

    [Fact]
    public void ForeignCurrency_NullLambda_ThrowsArgumentNullException()
    {
        var act = () => VatCalculationEngine.For<Invoice, InvoiceLine>(cfg => cfg
            .Document(doc => doc
                .LineItems(f => f.Lines)
                .Method(f => f.CalcMethod)
                .ForeignCurrency((Func<Invoice, ValueObjects.ExchangeRate>)null!))
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .Quantity(p => p.Qty)
                .VatRate(p => p.Vat)));

        act.Should().Throw<ArgumentNullException>();
    }
}
