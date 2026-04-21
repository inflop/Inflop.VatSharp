using System.Globalization;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Samples;

/// <summary>
/// Shared table formatting helpers used by all demo classes.
/// Uses InvariantCulture for all decimal formatting so output is consistent
/// regardless of the host machine's regional settings.
/// </summary>
internal static class ConsoleWriter
{
    internal static void Header(int number, string title)
    {
        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════════════════");
        Console.WriteLine($" Demo {number:D2} — {title}");
        Console.WriteLine("══════════════════════════════════════════════════════");
    }

    internal static void SubHeader(string text)
    {
        Console.WriteLine();
        Console.WriteLine($"  {text}");
        Console.WriteLine($"  {new string('─', text.Length)}");
    }

    internal static void PrintDocumentAmounts(DocumentAmounts result, string invoiceLabel)
    {
        Console.WriteLine();
        Console.WriteLine($"  Invoice : {invoiceLabel}");
        Console.WriteLine($"  Method  : {result.Method}");
        Console.WriteLine();
        Console.WriteLine("  VAT Rate  │     Net │     VAT │    Gross");
        Console.WriteLine("  ──────────┼─────────┼─────────┼─────────");
        foreach (var s in result.VatRateSummaries)
        {
            Console.WriteLine($"  {s.VatRate,8}  │ {F(s.TotalNet),7} │ {F(s.TotalVat),7} │ {F(s.TotalGross),7}");
        }
        Console.WriteLine("  ──────────┼─────────┼─────────┼─────────");
        Console.WriteLine($"  {"Total",-8}  │ {F(result.TotalNet),7} │ {F(result.TotalVat),7} │ {F(result.TotalGross),7}");
        if (!result.TotalDiscount.IsZero)
            Console.WriteLine($"  Discount  │         │         │ -{F(result.TotalDiscount),6}");
    }

    internal static void PrintLineItems(IReadOnlyList<LineItemAmounts> items, IReadOnlyList<string> descriptions)
    {
        Console.WriteLine();
        Console.WriteLine("  Line items:");
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var disc = item.DiscountAmount.IsZero ? "" : $"  (disc -{F(item.DiscountAmount)})";
            Console.WriteLine($"  [{i + 1}] {descriptions[i],-38}  net {F(item.NetValue),7}  vat {F(item.VatAmount),6}  gross {F(item.GrossValue),7}{disc}");
        }
    }

    internal static void PrintFcyDocumentAmounts(ForeignCurrencyDocumentAmounts result, string invoiceLabel)
    {
        Console.WriteLine();
        Console.WriteLine($"  Invoice       : {invoiceLabel}");
        Console.WriteLine($"  Currency      : {result.Currency}");
        Console.WriteLine($"  Exchange rate : {result.ExchangeRate}");
        Console.WriteLine($"  Method        : {result.Method}");
        Console.WriteLine();

        Console.WriteLine($"  {"VAT Rate",-8}  │ {"Net (fcy)",9} │ {"VAT (fcy)",9} │ {"Gross (fcy)",11} │ {"Net (base)",10} │ {"VAT (base)",10} │ {"Gross (base)",12}");
        Console.WriteLine($"  {"─────────",-8}  ┼ {"─────────",9} ┼ {"─────────",9} ┼ {"───────────",11} ┼ {"──────────",10} ┼ {"──────────",10} ┼ {"────────────",12}");
        foreach (var s in result.VatRateSummaries)
        {
            Console.WriteLine($"  {s.VatRate,-8}  │ {F(s.TotalNet),9} │ {F(s.TotalVat),9} │ {F(s.TotalGross),11} │ {F(s.TotalNetBase),10} │ {F(s.TotalVatBase),10} │ {F(s.TotalGrossBase),12}");
        }
        Console.WriteLine($"  {"─────────",-8}  ┼ {"─────────",9} ┼ {"─────────",9} ┼ {"───────────",11} ┼ {"──────────",10} ┼ {"──────────",10} ┼ {"────────────",12}");
        Console.WriteLine($"  {"Total",-8}  │ {F(result.TotalNet),9} │ {F(result.TotalVat),9} │ {F(result.TotalGross),11} │ {F(result.TotalNetBase),10} │ {F(result.TotalVatBase),10} │ {F(result.TotalGrossBase),12}");
    }

    // Formats a Money value with 2 decimal places, invariant culture.
    internal static string F(Money m) => m.Value.ToString("F2", CultureInfo.InvariantCulture);
}
