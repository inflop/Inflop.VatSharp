using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Exceptions;
using Inflop.VatSharp.Samples.Data;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Samples.Demos;

/// <summary>
/// Demo 04 — Calculation Methods Comparison.
///
/// Three methods defined in art. 226 of Directive 2006/112/EC:
///   Method I   (FromSumOfNetValues)      — authoritative field: net. Standard B2B.
///   Method II  (FromSumOfGrossValues)    — authoritative field: gross. Retail/fiscal.
///   Method III (SumOfLineItemVatAmounts) — per-line VAT rounded then summed.
///
/// Methods I and III produce identical totals on exact data but may differ by ±0.01
/// due to rounding order. The rounding difference section below makes this visible.
/// </summary>
internal static class CalculationMethodsDemo
{
    public static void Run()
    {
        ConsoleWriter.Header(4, "Calculation Methods Comparison");

        // ── Method I: FromSumOfNetValues on OfficeSuppliesInvoice ─────────────
        ConsoleWriter.SubHeader("Method I — FromSumOfNetValues  (net prices, B2B standard)");

        var netEngine = VatCalculationEngine.ForItems<LineItem>(l => l
            .NetUnitPrice(li => li.Price)
            .Quantity(li => li.Qty)
            .VatRate(li => li.VatRate));

        var officeInv = SampleData.OfficeSuppliesInvoice;
        var methodI   = netEngine.Calculate(officeInv.Lines, VatCalculationMethod.FromSumOfNetValues);
        ConsoleWriter.PrintDocumentAmounts(methodI, officeInv.Number);

        // ── Method II: FromSumOfGrossValues on RetailBasketInvoice ────────────
        ConsoleWriter.SubHeader("Method II — FromSumOfGrossValues  (gross prices, retail)");

        var grossEngine = VatCalculationEngine.ForItems<LineItem>(l => l
            .GrossUnitPrice(li => li.Price)   // price type is gross
            .Quantity(li => li.Qty)
            .VatRate(li => li.VatRate));

        var retailInv = SampleData.RetailBasketInvoice;
        var methodII  = grossEngine.Calculate(retailInv.Lines, VatCalculationMethod.FromSumOfGrossValues);
        ConsoleWriter.PrintDocumentAmounts(methodII, retailInv.Number);

        // Calling FromSumOfGrossValues on a net-mapped engine throws VatCalculationException.
        // The strategy requires all items to carry a gross unit price (IsGross = true).
        // netEngine maps with NetUnitPrice → IsNet = true → the strategy rejects them.
        Console.WriteLine();
        Console.WriteLine("  Calling FromSumOfGrossValues on net-mapped items throws VatCalculationException:");
        try
        {
            netEngine.Calculate(officeInv.Lines, VatCalculationMethod.FromSumOfGrossValues);
        }
        catch (VatCalculationException ex)
        {
            Console.WriteLine($"  Caught: {ex.Message}");
        }

        // ── Method III: SumOfLineItemVatAmounts ───────────────────────────────
        ConsoleWriter.SubHeader("Method III — SumOfLineItemVatAmounts  (per-line VAT rounded)");
        var methodIII = netEngine.Calculate(officeInv.Lines, VatCalculationMethod.SumOfLineItemVatAmounts);
        ConsoleWriter.PrintDocumentAmounts(methodIII, officeInv.Number);

        // ── Rounding difference: same data, Methods I vs III ──────────────────
        ConsoleWriter.SubHeader("Rounding difference: 10 lines @23%, prices 10.01..10.10");

        var roundingItems = Enumerable.Range(1, 10)
            .Select(i => new InvoiceLineItem(
                UnitPrice: UnitPrice.Net(10.00m + i * 0.01m),
                Quantity:  Quantity.Of(1),
                VatRate:   VatRate.Of(23)))
            .ToArray();

        var directEngine = VatCalculationEngine.Create();
        var rdMethodI    = directEngine.Calculate(roundingItems, VatCalculationMethod.FromSumOfNetValues);
        var rdMethodIII  = directEngine.Calculate(roundingItems, VatCalculationMethod.SumOfLineItemVatAmounts);

        Console.WriteLine();
        Console.WriteLine($"  Method I  total VAT : {ConsoleWriter.F(rdMethodI.TotalVat)}");
        Console.WriteLine($"  Method III total VAT: {ConsoleWriter.F(rdMethodIII.TotalVat)}");
        Console.WriteLine($"  Difference          : {(rdMethodI.TotalVat.Value - rdMethodIII.TotalVat.Value):+0.00;-0.00;0.00}");
        Console.WriteLine();
        Console.WriteLine("  Both are legally correct per art. 226 of Directive 2006/112/EC.");
        Console.WriteLine("  Use Method I (net) for B2B; Method II (gross) for retail/fiscal receipt.");
    }
}
