using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Samples.Data;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Samples.Demos;

/// <summary>
/// Demo 06 — Foreign Currency (FCY).
///
/// Directive 2006/112/EC art. 91: where amounts are expressed in a foreign currency,
/// the taxable amount must be converted to the settlement currency using the rate
/// applicable on the date tax becomes chargeable.
///
/// Four topics:
///   ExchangeRate creation — factory method vs fluent builder.
///   Pattern 1 — Rate supplied by caller at call time.
///   Pattern 2 — Rate mapped from the document field (.ForeignCurrency(d => d.Rate)).
///   Pattern 3 — Constant rate for a batch (.ForeignCurrency(staticRate)).
/// </summary>
internal static class ForeignCurrencyDemo
{
    public static void Run()
    {
        ConsoleWriter.Header(6, "Foreign Currency");

        var inv  = SampleData.SoftwareServicesInvoice;
        var rate = SampleData.EurPlnRate;

        // ── ExchangeRate creation ─────────────────────────────────────────────
        ConsoleWriter.SubHeader("ExchangeRate creation: factory method and fluent builder");

        // Factory method — all parameters in one call:
        ExchangeRate rateFactory = ExchangeRate.Of(
            CurrencyCode.Of("EUR"),
            CurrencyCode.Of("PLN"),
            4.2456m,
            new DateOnly(2026, 3, 28),
            "ECB");

        // Fluent builder — discovers parameters step-by-step.
        // ExchangeRateSpec carries an implicit conversion to ExchangeRate.
        ExchangeRate rateFluent = ExchangeRate
            .From(CurrencyCode.Of("EUR"))
            .To(CurrencyCode.Of("PLN"))
            .Rate(4.2456m)
            .Date(new DateOnly(2026, 3, 28))
            .Source("ECB");

        Console.WriteLine();
        Console.WriteLine($"  Factory : {rateFactory}");
        Console.WriteLine($"  Fluent  : {rateFluent}");
        Console.WriteLine($"  Equal   : {rateFactory == rateFluent}");

        // ── Pattern 1: Rate supplied by caller ────────────────────────────────
        ConsoleWriter.SubHeader("Pattern 1 — Rate supplied by caller at call time");

        // ForItems engine — rate passed to Calculate().
        var itemEngine = VatCalculationEngine.ForItems<LineItem>(l => l
            .NetUnitPrice(li => li.Price)
            .Quantity(li => li.Qty)
            .VatRate(li => li.VatRate));

        var result1 = itemEngine.Calculate(inv.Lines, VatCalculationMethod.FromSumOfNetValues, rate);
        ConsoleWriter.PrintFcyDocumentAmounts(result1, $"{inv.Number} — Software Services");

        // ToBaseDocumentAmounts(): project to PLN-only for VAT declaration.
        var base1 = result1.ToBaseDocumentAmounts();
        Console.WriteLine();
        Console.WriteLine($"  Base currency (PLN) totals for VAT declaration:");
        Console.WriteLine($"    Net {ConsoleWriter.F(base1.TotalNet)}  VAT {ConsoleWriter.F(base1.TotalVat)}  Gross {ConsoleWriter.F(base1.TotalGross)}");
        Console.WriteLine("  (LineItems remain in invoice currency — Directive 2006/112/EC art. 91)");

        // ── Pattern 2: Rate mapped from document field ────────────────────────
        ConsoleWriter.SubHeader("Pattern 2 — Rate mapped from document field (.ForeignCurrency(d => d.Rate))");

        var docEngine2 = VatCalculationEngine.For<Invoice, LineItem>(cfg => cfg
            .Document(d => d
                .LineItems(i => i.Lines)
                .Method(VatCalculationMethod.FromSumOfNetValues)
                .ForeignCurrency(i => i.Rate!))   // Rate is guaranteed non-null for FCY invoices
            .LineItem(l => l
                .NetUnitPrice(li => li.Price)
                .Quantity(li => li.Qty)
                .VatRate(li => li.VatRate)));

        var result2 = docEngine2.CalculateFcy(inv);
        ConsoleWriter.PrintFcyDocumentAmounts(result2, $"{inv.Number} — from document field");

        // ── Pattern 3: Constant rate for a batch ──────────────────────────────
        ConsoleWriter.SubHeader("Pattern 3 — Constant rate for a batch (.ForeignCurrency(staticRate))");

        // A single engine instance handles a batch of same-currency invoices
        // all settled at a fixed monthly rate (e.g. central-bank average for the month).
        var docEngine3 = VatCalculationEngine.For<Invoice, LineItem>(cfg => cfg
            .Document(d => d
                .LineItems(i => i.Lines)
                .Method(VatCalculationMethod.FromSumOfNetValues)
                .ForeignCurrency(rate))             // constant rate baked into the engine
            .LineItem(l => l
                .NetUnitPrice(li => li.Price)
                .Quantity(li => li.Qty)
                .VatRate(li => li.VatRate)));

        // In a real scenario: invoices.Select(docEngine3.CalculateFcy)
        var result3 = docEngine3.CalculateFcy(inv);
        ConsoleWriter.PrintFcyDocumentAmounts(result3, $"{inv.Number} — constant batch rate");
    }
}
