using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Samples.Data;

namespace Inflop.VatSharp.Samples.Demos;

/// <summary>
/// Demo 03 — Fluent Mapping, Items Only (no document wrapper).
/// ForItems&lt;T&gt;() skips the document type — method is passed at call time.
/// CalculateLineItem() is useful for live preview during data entry.
/// </summary>
internal static class FluentMappingItemsOnlyDemo
{
    public static void Run()
    {
        ConsoleWriter.Header(3, "Fluent Mapping — Items Only (No Document Wrapper)");

        // ForItems<T>() — no document type required.
        // The calculation method is passed at call time, not embedded in the engine.
        var engine = VatCalculationEngine.ForItems<LineItem>(l => l
            .NetUnitPrice(li => li.Price)
            .Quantity(li => li.Qty)
            .VatRate(li => li.VatRate));

        var inv = SampleData.OfficeSuppliesInvoice;

        // Calculate all lines at once.
        var result = engine.Calculate(inv.Lines, VatCalculationMethod.FromSumOfNetValues);
        ConsoleWriter.PrintDocumentAmounts(result, $"{inv.Number} — Office Supplies");

        // CalculateLineItem: single-line preview — useful in UI editing scenarios.
        ConsoleWriter.SubHeader("Single-line preview (CalculateLineItem)");
        var singleLine = inv.Lines.First();
        var lineResult = engine.CalculateLineItem(singleLine);
        Console.WriteLine();
        Console.WriteLine($"  [{singleLine.Description}]");
        Console.WriteLine($"    net {ConsoleWriter.F(lineResult.NetValue)}  vat {ConsoleWriter.F(lineResult.VatAmount)}  gross {ConsoleWriter.F(lineResult.GrossValue)}");
    }
}
