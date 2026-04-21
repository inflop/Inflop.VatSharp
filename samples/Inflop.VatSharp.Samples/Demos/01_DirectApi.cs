using Inflop.VatSharp.Samples.Data;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Samples.Demos;

/// <summary>
/// Demo 01 — Direct API: shortest path to a VAT calculation result.
/// No mapping configuration. Uses the library's own InvoiceLineItem value objects.
/// </summary>
internal static class DirectApiDemo
{
    public static void Run()
    {
        ConsoleWriter.Header(1, "Direct API");

        // Build InvoiceLineItem[] directly from the library's value objects.
        // UnitPrice.Net() wraps a decimal as a net price.
        // Quantity.Of() and VatRate.Of() wrap their numeric arguments.
        var inv = SampleData.OfficeSuppliesInvoice;
        var items = inv.Lines
            .Select(l => new InvoiceLineItem(
                UnitPrice:  l.IsGross ? UnitPrice.Gross(l.Price) : UnitPrice.Net(l.Price),
                Quantity:   Quantity.Of(l.Qty),
                VatRate:    VatRate.Of(l.VatRate)))
            .ToArray();

        var engine = VatCalculationEngine.Create();
        var result = engine.Calculate(items, Enums.VatCalculationMethod.SumOfLineItemVatAmounts);

        ConsoleWriter.PrintDocumentAmounts(result, $"{inv.Number} — Office Supplies");
        ConsoleWriter.PrintLineItems(result.LineItems, inv.Lines.Select(l => l.Description).ToList());
    }
}
