using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Samples.Data;

public static class SampleData
{
    // ── Exchange rates ───────────────────────────────────────────────────────

    public static readonly ExchangeRate EurPlnRate =
        ExchangeRate.Of(CurrencyCode.Of("EUR"), CurrencyCode.Of("PLN"), 4.2456m,
                        new DateOnly(2026, 3, 28), "ECB");

    public static readonly ExchangeRate EurJpyRate =
        ExchangeRate.Of(CurrencyCode.Of("EUR"), CurrencyCode.Of("JPY"), 161.47m,
                        new DateOnly(2026, 3, 28), "ECB");

    // ── Invoices ─────────────────────────────────────────────────────────────

    // B2B office supplies — 5 lines, 4 VAT rates (0%, 5%, 8%, 23%), net prices.
    // Total: net 233.30, VAT 42.29, gross 275.59 (SumOfLineItemVatAmounts).
    public static readonly Invoice OfficeSuppliesInvoice = new()
    {
        Number     = "INV/2026/03/001",
        CalcMethod = "net",
        Lines      =
        [
            new() { Description = "Copy paper A4 (500 sheets)",      Price = 12.50m, Qty = 10, VatRate = 23 },
            new() { Description = "Coffee capsules Arabica",          Price =  8.90m, Qty =  5, VatRate = 23 },
            new() { Description = "Still mineral water 1.5L",         Price =  4.80m, Qty =  6, VatRate =  8 },
            new() { Description = "Whole grain bread (catering)",     Price =  4.00m, Qty =  5, VatRate =  5 },
            new() { Description = "Postal service",                   Price = 15.00m, Qty =  1, VatRate =  0 },
        ]
    };

    // B2B IT equipment — 3 lines, 23%, with discounts.
    // Line 1: laptop 1 800.00 × 1, -10% percentage discount.
    // Line 2: USB hub 149.00 × 2, -30.00 absolute discount (from total).
    // Line 3: extended warranty 50.00 × 3, no discount.
    public static readonly Invoice ItEquipmentInvoice = new()
    {
        Number     = "INV/2026/03/002",
        CalcMethod = "net",
        Lines      =
        [
            new() { Description = "Laptop ProBook 450 G11",      Price = 1800.00m, Qty = 1, VatRate = 23, DiscountPct = 10m },
            new() { Description = "USB-C Hub 7-in-1",            Price =  149.00m, Qty = 2, VatRate = 23, DiscountAmt = 30m },
            new() { Description = "Extended warranty 36 months", Price =   50.00m, Qty = 3, VatRate = 23 },
        ]
    };

    // Retail grocery basket — 3 lines, gross prices (retail scenario).
    public static readonly Invoice RetailBasketInvoice = new()
    {
        Number     = "INV/2026/03/003",
        CalcMethod = "gross",
        Lines      =
        [
            new() { Description = "Whole grain bread 500g",   Price =  2.50m, IsGross = true, Qty = 4, VatRate =  5 },
            new() { Description = "Still mineral water 1.5L", Price =  1.89m, IsGross = true, Qty = 6, VatRate =  8 },
            new() { Description = "Ground coffee 250g",       Price =  8.99m, IsGross = true, Qty = 2, VatRate = 23 },
        ]
    };

    // B2B software services — EUR, 23%. Rate field used for FCY demos.
    public static readonly Invoice SoftwareServicesInvoice = new()
    {
        Number     = "INV/2026/03/004",
        CalcMethod = "net",
        Rate       = EurPlnRate,
        Lines      =
        [
            new() { Description = "Cloud platform subscription (monthly)", Price = 299.00m, Qty = 1, VatRate = 23 },
            new() { Description = "Implementation services",               Price = 450.00m, Qty = 2, VatRate = 23 },
        ]
    };
}
