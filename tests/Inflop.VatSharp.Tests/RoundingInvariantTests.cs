using FluentAssertions;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Mapping;
using Inflop.VatSharp.ValueObjects;
using Xunit;

namespace Inflop.VatSharp.Tests;

// ═══════════════════════════════════════════════════════════════════════════
//  Rounding behavior in calculation pipeline.
//  Defends against silent regression to Banker's rounding (ToEven).
//  Polish VAT law (ustawa o VAT) and EU Directive 2006/112/EC require
//  AwayFromZero rounding for tax amounts at midpoints.
// ═══════════════════════════════════════════════════════════════════════════

public class RoundingInvariantTests
{
    private readonly LineItemCalculationEngine<InvoiceLineItem> _engine = VatCalculationEngine.Create();

    [Fact]
    public void MethodI_MidpointVat_RoundsAwayFromZero_NotBankersRounding()
    {
        // Net(1.00) × 0.5% = 0.005m exact midpoint
        // AwayFromZero → 0.01;  Banker's → 0.00
        var item = new InvoiceLineItem(UnitPrice.Net(1m), Quantity.Of(1), VatRate.Of(0.5m));

        var result = _engine.Calculate([item], VatCalculationMethod.FromSumOfNetValues);

        result.TotalVat.Value.Should().Be(0.01m);
    }

    [Fact]
    public void MethodII_MidpointVat_RoundsAwayFromZero_NotBankersRounding()
    {
        // Gross(1.005) × 0.5/100.5 = 0.005m exact midpoint
        //   With AwayFromZero: itemGross.Round(1.005) = 1.01;
        //                      VatFromGross(1.01) = 0.005024..., round = 0.01
        //   With Banker's:     itemGross.Round(1.005) = 1.00;
        //                      VatFromGross(1.00) = 0.004975..., round = 0.00
        // Money.Of(1.005m) is allowed — Money does not enforce 2dp on input.
        var item = new InvoiceLineItem(UnitPrice.Gross(1.005m), Quantity.Of(1), VatRate.Of(0.5m));

        var result = _engine.Calculate([item], VatCalculationMethod.FromSumOfGrossValues);

        result.TotalVat.Value.Should().Be(0.01m);
    }

    [Fact]
    public void MethodIII_MidpointVat_PerLine_RoundsAwayFromZero()
    {
        var item = new InvoiceLineItem(UnitPrice.Net(1m), Quantity.Of(1), VatRate.Of(0.5m));

        var result = _engine.Calculate([item], VatCalculationMethod.SumOfLineItemVatAmounts);

        result.TotalVat.Value.Should().Be(0.01m);
        result.LineItems[0].VatAmount.Value.Should().Be(0.01m);
    }
}
