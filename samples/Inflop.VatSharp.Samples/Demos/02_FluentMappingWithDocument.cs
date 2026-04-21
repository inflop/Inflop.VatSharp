using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Samples.Data;

namespace Inflop.VatSharp.Samples.Demos;

/// <summary>
/// Demo 02 — Fluent Mapping with Document Wrapper.
/// Maps arbitrary Invoice/LineItem types to the engine.
/// Method is resolved dynamically from the document's CalcMethod field.
/// </summary>
internal static class FluentMappingWithDocumentDemo
{
    public static void Run()
    {
        ConsoleWriter.Header(2, "Fluent Mapping with Document Wrapper");

        // VatCalculationEngine.For<TDoc, TLine>() configures a typed engine.
        // Document(...) maps the line items accessor and the calculation method.
        // LineItem(...) maps the price, quantity, and VAT rate from the POCO fields.
        var engine = VatCalculationEngine.For<Invoice, LineItem>(cfg => cfg
            .Document(d => d
                .LineItems(inv => inv.Lines)
                .Method(inv => inv.CalcMethod switch
                {
                    "gross" => VatCalculationMethod.FromSumOfGrossValues,
                    "line"  => VatCalculationMethod.SumOfLineItemVatAmounts,
                    _       => VatCalculationMethod.FromSumOfNetValues,  // "net" (default)
                }))
            .LineItem(l => l
                .NetUnitPrice(li => li.Price)
                .Quantity(li => li.Qty)
                .VatRate(li => li.VatRate)));

        var inv = SampleData.OfficeSuppliesInvoice;
        var result = engine.Calculate(inv);

        ConsoleWriter.PrintDocumentAmounts(result, $"{inv.Number} — Office Supplies");
        ConsoleWriter.PrintLineItems(result.LineItems, inv.Lines.Select(l => l.Description).ToList());
    }
}
