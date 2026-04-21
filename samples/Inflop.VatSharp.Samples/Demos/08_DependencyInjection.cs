using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Mapping;
using Inflop.VatSharp.Samples.Data;
using Inflop.VatSharp.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Inflop.VatSharp.Samples.Demos;

/// <summary>
/// Demo 08 — Dependency Injection.
///
/// VatCalculationEngine&lt;TDoc,TLine&gt; is immutable and thread-safe — register as singleton.
/// Two registration patterns:
///   Pattern 1 — Direct factory: services.AddSingleton(VatCalculationEngine.For(...))
///   Pattern 2 — Via VatCalculationRegistration helper: useful when engine config is
///               maintained separately from the DI registration (e.g. in a module class).
/// </summary>
internal static class DependencyInjectionDemo
{
    public static void Run()
    {
        ConsoleWriter.Header(8, "Dependency Injection");

        // ── Pattern 1: Direct factory registration ────────────────────────────
        ConsoleWriter.SubHeader("Pattern 1 — Direct factory registration");

        var services1 = new ServiceCollection();

        // Register the engine as singleton — safe because the engine is immutable.
        services1.AddSingleton(VatCalculationEngine.For<Invoice, LineItem>(cfg => cfg
            .Document(d => d
                .LineItems(inv => inv.Lines)
                .Method(VatCalculationMethod.FromSumOfNetValues))
            .LineItem(l => l
                .NetUnitPrice(li => li.Price)
                .Quantity(li => li.Qty)
                .VatRate(li => li.VatRate))));

        services1.AddTransient<InvoiceService>();

        using var container1 = services1.BuildServiceProvider();
        var svc1    = container1.GetRequiredService<InvoiceService>();
        var result1 = svc1.Process(SampleData.OfficeSuppliesInvoice);
        ConsoleWriter.PrintDocumentAmounts(result1, SampleData.OfficeSuppliesInvoice.Number);

        // ── Pattern 2: VatCalculationRegistration helper ──────────────────────
        ConsoleWriter.SubHeader("Pattern 2 — VatCalculationRegistration helper");

        // VatCalculationRegistration.CreateEngine<TDoc, TLine>() is equivalent to
        // VatCalculationEngine.For<TDoc, TLine>() but makes intent explicit:
        // the method is named for registration use and is discoverable via IDE.
        var services2 = new ServiceCollection();

        services2.AddSingleton(VatCalculationRegistration.CreateEngine<Invoice, LineItem>(cfg => cfg
            .Document(d => d
                .LineItems(inv => inv.Lines)
                .Method(VatCalculationMethod.FromSumOfNetValues))
            .LineItem(l => l
                .NetUnitPrice(li => li.Price)
                .Quantity(li => li.Qty)
                .VatRate(li => li.VatRate))));

        services2.AddTransient<InvoiceService>();

        using var container2 = services2.BuildServiceProvider();
        var svc2    = container2.GetRequiredService<InvoiceService>();
        var result2 = svc2.Process(SampleData.OfficeSuppliesInvoice);

        Console.WriteLine();
        Console.WriteLine("  Pattern 2 result (same totals as Pattern 1):");
        Console.WriteLine($"    Net {ConsoleWriter.F(result2.TotalNet)}  VAT {ConsoleWriter.F(result2.TotalVat)}  Gross {ConsoleWriter.F(result2.TotalGross)}");

        Console.WriteLine();
        Console.WriteLine("  The engine is immutable — safe as a singleton across requests.");
    }
}

/// <summary>
/// Example application service using constructor injection (primary constructor syntax).
/// Defined inline so the demo file is self-contained.
/// </summary>
file sealed class InvoiceService(VatCalculationEngine<Invoice, LineItem> engine)
{
    public DocumentAmounts Process(Invoice invoice) => engine.Calculate(invoice);
}
