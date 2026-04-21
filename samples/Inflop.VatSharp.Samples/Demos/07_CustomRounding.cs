using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Samples.Data;
using Inflop.VatSharp.Strategies.Rounding;

namespace Inflop.VatSharp.Samples.Demos;

/// <summary>
/// Demo 07 — Custom Rounding.
///
/// IRoundingStrategy controls how VAT amounts are rounded after calculation.
/// The default is arithmetic rounding to 2dp (MidpointRounding.AwayFromZero).
///
/// Shown here:
///   1. DefaultRounding.TwoDecimalPlaces  — standard (EUR, PLN, GBP, …)
///   2. SwissRounding (custom)            — CHF rounds to nearest 0.05 (Rappenausgleich)
///   3. DefaultRounding.ZeroDecimalPlaces — HUF has no fractional unit
///   4. Base-currency rounding (JPY)      — EUR invoice, JPY settlement, 0dp base rounding
/// </summary>
internal static class CustomRoundingDemo
{
    public static void Run()
    {
        ConsoleWriter.Header(7, "Custom Rounding");

        var inv = SampleData.RetailBasketInvoice;

        // ── 1. Default 2dp ────────────────────────────────────────────────────
        ConsoleWriter.SubHeader("1 — Default (2 decimal places, EUR/PLN/GBP)");
        var defaultEngine = VatCalculationEngine.ForItems<LineItem>(l => l
            .GrossUnitPrice(li => li.Price)
            .Quantity(li => li.Qty)
            .VatRate(li => li.VatRate),
            rounding: DefaultRounding.TwoDecimalPlaces);
        var r1 = defaultEngine.Calculate(inv.Lines, VatCalculationMethod.FromSumOfGrossValues);
        ConsoleWriter.PrintDocumentAmounts(r1, $"{inv.Number} — default rounding");

        // ── 2. Swiss CHF rounding (0.05) ──────────────────────────────────────
        ConsoleWriter.SubHeader("2 — CHF: rounds to nearest 0.05 (Rappenausgleich)");
        var chfEngine = VatCalculationEngine.ForItems<LineItem>(l => l
            .GrossUnitPrice(li => li.Price)
            .Quantity(li => li.Qty)
            .VatRate(li => li.VatRate),
            rounding: new SwissRounding());
        var r2 = chfEngine.Calculate(inv.Lines, VatCalculationMethod.FromSumOfGrossValues);
        ConsoleWriter.PrintDocumentAmounts(r2, $"{inv.Number} — CHF rounding");

        // ── 3. HUF 0dp ────────────────────────────────────────────────────────
        ConsoleWriter.SubHeader("3 — HUF: zero decimal places (no fractional currency unit)");
        var hufEngine = VatCalculationEngine.ForItems<LineItem>(l => l
            .GrossUnitPrice(li => li.Price)
            .Quantity(li => li.Qty)
            .VatRate(li => li.VatRate),
            rounding: DefaultRounding.ZeroDecimalPlaces);
        var r3 = hufEngine.Calculate(inv.Lines, VatCalculationMethod.FromSumOfGrossValues);
        ConsoleWriter.PrintDocumentAmounts(r3, $"{inv.Number} — HUF rounding");

        // ── 4. Base-currency rounding (EUR invoice → JPY settlement) ──────────
        ConsoleWriter.SubHeader("4 — Base-currency rounding: EUR invoice settled in JPY (0dp)");

        // Invoice currency rounding stays at 2dp (EUR).
        // Base-currency rounding is overridden to 0dp (JPY has no fractional unit).
        var jpyEngine = VatCalculationEngine.ForItems<LineItem>(l => l
            .NetUnitPrice(li => li.Price)
            .Quantity(li => li.Qty)
            .VatRate(li => li.VatRate),
            rounding:             DefaultRounding.TwoDecimalPlaces,
            baseCurrencyRounding: DefaultRounding.ZeroDecimalPlaces);

        var swInv     = SampleData.SoftwareServicesInvoice;
        var jpyResult = jpyEngine.Calculate(swInv.Lines, VatCalculationMethod.FromSumOfNetValues, SampleData.EurJpyRate);
        ConsoleWriter.PrintFcyDocumentAmounts(jpyResult, $"{swInv.Number} — EUR→JPY, 0dp base rounding");

        // ── Side-by-side VAT comparison ───────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine("  Retail basket VAT comparison (same data, four rounding strategies):");
        Console.WriteLine($"    Default 2dp : {ConsoleWriter.F(r1.TotalVat)}");
        Console.WriteLine($"    CHF 0.05    : {ConsoleWriter.F(r2.TotalVat)}");
        Console.WriteLine($"    HUF 0dp     : {ConsoleWriter.F(r3.TotalVat)}");
    }
}

// SwissRounding: rounds to nearest 0.05 (Rappenausgleich — Swiss rounding convention).
// Defined inline so the demo is self-contained.
file sealed class SwissRounding : IRoundingStrategy
{
    public decimal Round(decimal value)
        => Math.Round(value / 0.05m, MidpointRounding.AwayFromZero) * 0.05m;

    public override string ToString() => "Swiss(0.05)";
}
