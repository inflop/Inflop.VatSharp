using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Samples.Data;
using Inflop.VatSharp.Strategies.Discount;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Samples.Demos;

/// <summary>
/// Demo 05 — Discounts.
///
/// Art. 79 lit. b of Directive 2006/112/EC: discounts granted at the time of supply
/// reduce the taxable amount.
///
/// Three discount types supported:
///   - Percentage: Discount.OfPercentage(10m) — % off the line total
///   - Absolute from-total: Discount.OfAmount(30m) — fixed amount off the line total (default)
///   - Absolute per-unit: same amount but applied per unit before qty multiplication
///
/// Three mapping modes (LineItemMappingBuilder):
///   - .DiscountPercentage(x => x.DiscountPct)   — decimal? field → percentage discount
///   - .DiscountAbsolute(x => x.DiscountAmt)      — decimal? field → absolute discount
///   - .Discount(x => x.Disc)                     — Discount? field → pass the value object directly
/// </summary>
internal static class DiscountsDemo
{
    public static void Run()
    {
        ConsoleWriter.Header(5, "Discounts");

        var inv = SampleData.ItEquipmentInvoice;

        // ── Mode 1 & 2: inline InvoiceLineItem[] with Direct API ──────────────
        ConsoleWriter.SubHeader("Inline InvoiceLineItem[] — percentage + absolute from-total (default)");

        var items = new[]
        {
            // Laptop: -10% percentage discount
            new InvoiceLineItem(
                UnitPrice: UnitPrice.Net(1800.00m),
                Quantity:  Quantity.Of(1),
                VatRate:   VatRate.Of(23),
                Discount:  Discount.OfPercentage(10m)),

            // USB Hub: -30.00 absolute discount from line total (default behavior)
            new InvoiceLineItem(
                UnitPrice: UnitPrice.Net(149.00m),
                Quantity:  Quantity.Of(2),
                VatRate:   VatRate.Of(23),
                Discount:  Discount.OfAmount(30.00m)),

            // Extended warranty: no discount
            new InvoiceLineItem(
                UnitPrice: UnitPrice.Net(50.00m),
                Quantity:  Quantity.Of(3),
                VatRate:   VatRate.Of(23)),
        };

        var engine          = VatCalculationEngine.Create();
        var resultFromTotal = engine.Calculate(items, VatCalculationMethod.FromSumOfNetValues);
        ConsoleWriter.PrintDocumentAmounts(resultFromTotal, $"{inv.Number} — IT Equipment");
        ConsoleWriter.PrintLineItems(resultFromTotal.LineItems, ["Laptop ProBook 450 G11", "USB-C Hub 7-in-1", "Extended warranty 36 months"]);

        // ── Per-unit absolute discount behavior ───────────────────────────────
        ConsoleWriter.SubHeader("Absolute discount per unit  (price − discount÷qty) × qty");

        // PerUnitAbsoluteDiscountBehavior distributes the discount per unit before multiplying.
        // Formula: (unitPrice − round(discount ÷ qty)) × qty
        //   From-total: qty × price − discount = 2 × 149 − 30 = 268   (default)
        //   Per-unit:   (price − round(30÷2)) × qty = (149 − 15) × 2 = 268
        // For this data both are equal (30 divides evenly by 2).
        // The difference appears when discount ÷ qty is not exact, e.g. discount=25, qty=2:
        //   From-total: 2×149 − 25 = 273   vs   Per-unit: (149 − 13) × 2 = 272
        var perUnitEngine = VatCalculationEngine.Create(discountBehavior: PerUnitAbsoluteDiscountBehavior.Instance);
        var resultPerUnit = perUnitEngine.Calculate(items, VatCalculationMethod.FromSumOfNetValues);
        ConsoleWriter.PrintDocumentAmounts(resultPerUnit, $"{inv.Number} — IT Equipment (per-unit discount)");

        Console.WriteLine();
        Console.WriteLine("  USB Hub line — from-total vs per-unit (30÷2=15 exact, so results are equal):");
        Console.WriteLine($"    From-total : net {ConsoleWriter.F(resultFromTotal.LineItems[1].NetValue)}  (2×149 − 30)");
        Console.WriteLine($"    Per-unit   : net {ConsoleWriter.F(resultPerUnit.LineItems[1].NetValue)}  ((149 − 30÷2) × 2 = (149−15)×2)");

        // ── Mode 3: .DiscountPercentage mapping ───────────────────────────────
        ConsoleWriter.SubHeader("Mapping mode: .DiscountPercentage(x => x.DiscountPct)");

        var pctEngine = VatCalculationEngine.ForItems<LineItem>(l => l
            .NetUnitPrice(li => li.Price)
            .Quantity(li => li.Qty)
            .VatRate(li => li.VatRate)
            .DiscountPercentage(li => li.DiscountPct));   // decimal? — null means no discount

        var pctResult = pctEngine.Calculate(inv.Lines, VatCalculationMethod.FromSumOfNetValues);
        ConsoleWriter.PrintDocumentAmounts(pctResult, $"{inv.Number} — percentage mapping");

        // ── Mode 4: .DiscountAbsolute mapping ─────────────────────────────────
        ConsoleWriter.SubHeader("Mapping mode: .DiscountAbsolute(x => x.DiscountAmt)");

        var absEngine = VatCalculationEngine.ForItems<LineItem>(l => l
            .NetUnitPrice(li => li.Price)
            .Quantity(li => li.Qty)
            .VatRate(li => li.VatRate)
            .DiscountAbsolute(li => li.DiscountAmt));  // decimal? — null means no discount

        var absResult = absEngine.Calculate(inv.Lines, VatCalculationMethod.FromSumOfNetValues);
        ConsoleWriter.PrintDocumentAmounts(absResult, $"{inv.Number} — absolute mapping");

        // ── Mode 5: .Discount(x => x.Disc) with a Discount? field ────────────
        ConsoleWriter.SubHeader("Mapping mode: .Discount(x => x.Disc) — Discount? value object");

        // For this mode we need a type that carries a Discount? field directly.
        // Defined as a local record to keep the demo self-contained.
        var discItems = new[]
        {
            new LineItemWithDiscount("Laptop ProBook 450 G11",     1800.00m, 1, 23, Discount.OfPercentage(10m)),
            new LineItemWithDiscount("USB-C Hub 7-in-1",            149.00m, 2, 23, Discount.OfAmount(30.00m)),
            new LineItemWithDiscount("Extended warranty 36 months",  50.00m, 3, 23, null),
        };

        var discEngine = VatCalculationEngine.ForItems<LineItemWithDiscount>(l => l
            .NetUnitPrice(li => li.Price)
            .Quantity(li => li.Qty)
            .VatRate(li => li.VatRate)
            .Discount(li => li.Disc));    // Discount? passed through directly

        var discResult = discEngine.Calculate(discItems, VatCalculationMethod.FromSumOfNetValues);
        ConsoleWriter.PrintDocumentAmounts(discResult, $"{inv.Number} — Discount? field mapping");
    }

    private sealed record LineItemWithDiscount(
        string    Description,
        decimal   Price,
        int       Qty,
        int       VatRate,
        Discount? Disc);
}
