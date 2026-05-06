using FluentAssertions;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.ValueObjects;
using Xunit;

namespace Inflop.VatSharp.Tests;

/// <summary>
/// Verifies that <see cref="VatRate"/> grouping in all calculation strategies treats
/// distinct zero-rate symbols ("0%", "ZW", "NP") as separate <see cref="VatRateSummary"/> rows.
///
/// Legal basis: art. 226 pts 8–10 of Directive 2006/112/EC and Polish JPK_V7 reporting
/// require legally distinct categories — zero-rated (art. 83 ustawy o VAT), exempt (art. 43),
/// and not-subject (NP / reverse charge) — to be reported separately even though all share
/// Percentage = 0.
/// </summary>
public class VatRateGroupingTests
{
    // ── FromSumOfNetValues (Method I) ───────────────────────────────────────

    [Fact]
    public void Calculate_FromSumOfNetValues_DistinctZeroSymbols_ProduceSeparateSummaries()
    {
        // Three legally distinct zero-rate categories on one document, plus one 23% line:
        //   "0%" → 100 net, no VAT, gross 100
        //   "ZW" → 200 net, no VAT, gross 200
        //   "NP" → 300 net, no VAT, gross 300
        //   "23%" → 1000 net, VAT 230, gross 1230
        // Expected: 4 distinct VatRateSummary rows (NOT 1 collapsed zero-rate row)
        var engine = VatCalculationEngine.Create();
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(100m),  Quantity.One, VatRate.Of(0m, "0%")),
            new(UnitPrice.Net(200m),  Quantity.One, VatRate.Of(0m, "ZW")),
            new(UnitPrice.Net(300m),  Quantity.One, VatRate.Of(0m, "NP")),
            new(UnitPrice.Net(1000m), Quantity.One, VatRate.Of(23)),
        };

        var result = engine.Calculate(items, VatCalculationMethod.FromSumOfNetValues);

        result.VatRateSummaries.Should().HaveCount(4);

        var zeroRated = result.VatRateSummaries.Single(s => s.VatRate.Symbol == "0%");
        zeroRated.TotalNet.Value.Should().Be(100m);
        zeroRated.TotalVat.Value.Should().Be(0m);
        zeroRated.TotalGross.Value.Should().Be(100m);

        var exempt = result.VatRateSummaries.Single(s => s.VatRate.Symbol == "ZW");
        exempt.TotalNet.Value.Should().Be(200m);
        exempt.TotalVat.Value.Should().Be(0m);
        exempt.TotalGross.Value.Should().Be(200m);

        var notSubject = result.VatRateSummaries.Single(s => s.VatRate.Symbol == "NP");
        notSubject.TotalNet.Value.Should().Be(300m);
        notSubject.TotalVat.Value.Should().Be(0m);
        notSubject.TotalGross.Value.Should().Be(300m);

        var standard = result.VatRateSummaries.Single(s => s.VatRate.Symbol == "23%");
        standard.TotalNet.Value.Should().Be(1000m);
        standard.TotalVat.Value.Should().Be(230m);    // 1000 × 23% = 230
        standard.TotalGross.Value.Should().Be(1230m); // 1000 + 230

        // Document-level aggregates: 100 + 200 + 300 + 1000 = 1600 net, only the 23% line carries VAT
        result.TotalNet.Value.Should().Be(1600m);
        result.TotalVat.Value.Should().Be(230m);
        result.TotalGross.Value.Should().Be(1830m);
    }

    // ── FromSumOfGrossValues (Method II) ────────────────────────────────────

    [Fact]
    public void Calculate_FromSumOfGrossValues_DistinctZeroSymbols_ProduceSeparateSummaries()
    {
        // Strategy II requires UnitPrice.Gross() for every line.
        // Zero-rate lines: gross == net (no VAT), so gross prices match the net targets:
        //   "0%" → gross 100 → net 100, VAT 0
        //   "ZW" → gross 200 → net 200, VAT 0
        //   "NP" → gross 300 → net 300, VAT 0
        //   "23%" → gross 1230 → VAT = 1230 × 23/123 = 230, net = 1000
        var engine = VatCalculationEngine.Create();
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Gross(100m),  Quantity.One, VatRate.Of(0m, "0%")),
            new(UnitPrice.Gross(200m),  Quantity.One, VatRate.Of(0m, "ZW")),
            new(UnitPrice.Gross(300m),  Quantity.One, VatRate.Of(0m, "NP")),
            new(UnitPrice.Gross(1230m), Quantity.One, VatRate.Of(23)),
        };

        var result = engine.Calculate(items, VatCalculationMethod.FromSumOfGrossValues);

        result.VatRateSummaries.Should().HaveCount(4);

        var zeroRated = result.VatRateSummaries.Single(s => s.VatRate.Symbol == "0%");
        zeroRated.TotalNet.Value.Should().Be(100m);
        zeroRated.TotalVat.Value.Should().Be(0m);
        zeroRated.TotalGross.Value.Should().Be(100m);

        var exempt = result.VatRateSummaries.Single(s => s.VatRate.Symbol == "ZW");
        exempt.TotalNet.Value.Should().Be(200m);
        exempt.TotalVat.Value.Should().Be(0m);
        exempt.TotalGross.Value.Should().Be(200m);

        var notSubject = result.VatRateSummaries.Single(s => s.VatRate.Symbol == "NP");
        notSubject.TotalNet.Value.Should().Be(300m);
        notSubject.TotalVat.Value.Should().Be(0m);
        notSubject.TotalGross.Value.Should().Be(300m);

        var standard = result.VatRateSummaries.Single(s => s.VatRate.Symbol == "23%");
        standard.TotalGross.Value.Should().Be(1230m);
        standard.TotalVat.Value.Should().Be(230m);   // 1230 × 23/123 = 230 (exact)
        standard.TotalNet.Value.Should().Be(1000m);  // 1230 − 230

        // Aggregates: 100 + 200 + 300 + 1000 = 1600 net, 230 VAT, 1830 gross
        result.TotalNet.Value.Should().Be(1600m);
        result.TotalVat.Value.Should().Be(230m);
        result.TotalGross.Value.Should().Be(1830m);
    }

    // ── SumOfLineItemVatAmounts (Method III) ────────────────────────────────

    [Fact]
    public void Calculate_SumOfLineItemVatAmounts_DistinctZeroSymbols_ProduceSeparateSummaries()
    {
        // Method III rounds VAT per line then sums; for zero-rate lines per-line VAT = 0,
        // so totals match Method I exactly. The 23% line contributes 230 VAT (100 × 23%).
        var engine = VatCalculationEngine.Create();
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(100m),  Quantity.One, VatRate.Of(0m, "0%")),
            new(UnitPrice.Net(200m),  Quantity.One, VatRate.Of(0m, "ZW")),
            new(UnitPrice.Net(300m),  Quantity.One, VatRate.Of(0m, "NP")),
            new(UnitPrice.Net(1000m), Quantity.One, VatRate.Of(23)),
        };

        var result = engine.Calculate(items, VatCalculationMethod.SumOfLineItemVatAmounts);

        result.VatRateSummaries.Should().HaveCount(4);

        var zeroRated = result.VatRateSummaries.Single(s => s.VatRate.Symbol == "0%");
        zeroRated.TotalNet.Value.Should().Be(100m);
        zeroRated.TotalVat.Value.Should().Be(0m);
        zeroRated.TotalGross.Value.Should().Be(100m);

        var exempt = result.VatRateSummaries.Single(s => s.VatRate.Symbol == "ZW");
        exempt.TotalNet.Value.Should().Be(200m);
        exempt.TotalVat.Value.Should().Be(0m);
        exempt.TotalGross.Value.Should().Be(200m);

        var notSubject = result.VatRateSummaries.Single(s => s.VatRate.Symbol == "NP");
        notSubject.TotalNet.Value.Should().Be(300m);
        notSubject.TotalVat.Value.Should().Be(0m);
        notSubject.TotalGross.Value.Should().Be(300m);

        var standard = result.VatRateSummaries.Single(s => s.VatRate.Symbol == "23%");
        standard.TotalNet.Value.Should().Be(1000m);
        standard.TotalVat.Value.Should().Be(230m);   // 1000 × 23% = 230 (exact, no rounding)
        standard.TotalGross.Value.Should().Be(1230m);

        result.TotalNet.Value.Should().Be(1600m);
        result.TotalVat.Value.Should().Be(230m);
        result.TotalGross.Value.Should().Be(1830m);
    }

    // ── Symbol preservation regression ──────────────────────────────────────

    [Theory]
    [InlineData(VatCalculationMethod.FromSumOfNetValues)]
    [InlineData(VatCalculationMethod.SumOfLineItemVatAmounts)]
    public void Calculate_NetMethods_PreservesSymbolOnVatRateSummary(VatCalculationMethod method)
    {
        // Regression: the Symbol on grouped VatRateSummary.VatRate must match the input Symbol
        // verbatim. Useful for downstream JPK_V7 / e-invoice serialization that keys on Symbol.
        var engine = VatCalculationEngine.Create();
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Net(100m), Quantity.One, VatRate.Of(0m, "0%")),
            new(UnitPrice.Net(200m), Quantity.One, VatRate.Of(0m, "ZW")),
            new(UnitPrice.Net(300m), Quantity.One, VatRate.Of(0m, "NP")),
        };

        var result = engine.Calculate(items, method);

        // Symbol-keyed lookup must return the exact net value supplied for that category
        result.VatRateSummaries.Single(s => s.VatRate.Symbol == "ZW").TotalNet.Value.Should().Be(200m);
        result.VatRateSummaries.Single(s => s.VatRate.Symbol == "NP").TotalNet.Value.Should().Be(300m);
        result.VatRateSummaries.Single(s => s.VatRate.Symbol == "0%").TotalNet.Value.Should().Be(100m);
    }

    [Fact]
    public void Calculate_GrossMethod_PreservesSymbolOnVatRateSummary()
    {
        // Same regression for gross-priced inputs (Method II requires UnitPrice.Gross()).
        // Zero-rate lines: gross == net, so gross input matches the expected per-symbol net total.
        var engine = VatCalculationEngine.Create();
        var items = new InvoiceLineItem[]
        {
            new(UnitPrice.Gross(100m), Quantity.One, VatRate.Of(0m, "0%")),
            new(UnitPrice.Gross(200m), Quantity.One, VatRate.Of(0m, "ZW")),
            new(UnitPrice.Gross(300m), Quantity.One, VatRate.Of(0m, "NP")),
        };

        var result = engine.Calculate(items, VatCalculationMethod.FromSumOfGrossValues);

        result.VatRateSummaries.Single(s => s.VatRate.Symbol == "ZW").TotalNet.Value.Should().Be(200m);
        result.VatRateSummaries.Single(s => s.VatRate.Symbol == "NP").TotalNet.Value.Should().Be(300m);
        result.VatRateSummaries.Single(s => s.VatRate.Symbol == "0%").TotalNet.Value.Should().Be(100m);
    }
}
