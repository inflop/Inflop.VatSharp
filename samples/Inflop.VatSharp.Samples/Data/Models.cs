using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Samples.Data;

// Minimal POCOs that represent a typical application's domain types.
// Note: the price field is named Price (not UnitPrice) to avoid a naming
// conflict with the library's ValueObjects.UnitPrice type.

public class Invoice
{
    public string          Number     { get; set; } = "";
    public List<LineItem>  Lines      { get; set; } = [];
    public ExchangeRate?   Rate       { get; set; }   // FCY scenarios only — Demo 06
    public string          CalcMethod { get; set; } = "net"; // "net" | "gross" | "line"
}

public class LineItem
{
    public string   Description { get; set; } = "";
    public decimal  Price       { get; set; }   // unit price
    public bool     IsGross     { get; set; }   // false = net price (default)
    public decimal  Qty         { get; set; }
    public int      VatRate     { get; set; }
    public decimal? DiscountPct { get; set; }   // percentage 0–100
    public decimal? DiscountAmt { get; set; }   // absolute monetary amount
}
