# Inflop.VatSharp

![Inflop.VatSharp Icon](https://raw.githubusercontent.com/inflop/Inflop.VatSharp/master/assets/vatsharp-icon-128.png)

[![NuGet](https://img.shields.io/nuget/v/Inflop.VatSharp.svg?logo=nuget&logoColor=white)](https://www.nuget.org/packages/Inflop.VatSharp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Inflop.VatSharp.svg?logo=nuget&logoColor=white)](https://www.nuget.org/packages/Inflop.VatSharp)
[![CI](https://github.com/inflop/VatSharp/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/inflop/VatSharp/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4)](https://dotnet.microsoft.com/)
[![Claude Code](https://img.shields.io/badge/Claude%20Code-assisted-D97757?logo=claude&logoColor=white)](https://claude.com/claude-code)

EU VAT calculation library compliant with **Council Directive 2006/112/EC** — three legally mandated calculation methods, pluggable rounding, foreign-currency support, and a fluent mapping API for zero-friction integration with existing codebases.

## Features

- **Three calculation methods** — net-sum, gross-sum, and per-line VAT (art. 226 of Directive 2006/112/EC)
- **Zero-friction integration** — fluent mapping engine binds the library to any existing domain model without interfaces, attributes, or inheritance
- **Foreign currency** — dual-currency results for VAT declarations (art. 91), with per-strategy base-currency conversion
- **Discounts** — percentage and absolute, at line level (art. 79 lit. b); configurable per-unit vs. from-total behavior
- **Pluggable rounding** — arithmetic by default (DE §14 UStG, NL art. 5a); customize for CHF 0.05, HUF whole units, JPY, KWD mills, etc.
- **Immutable, thread-safe** — engine is safe to register as a DI singleton
- **Zero dependencies** — library has no transitive NuGet packages
- **Multi-target** — .NET 8.0, 9.0, 10.0

## Table of contents

- [Installation](#installation)
- [Quick start](#quick-start)
- [Fluent mapping engine](#fluent-mapping-engine)
- [Discounts](#discounts)
- [Foreign currency](#foreign-currency)
- [Custom rounding](#custom-rounding)
- [Dependency injection](#dependency-injection)
- [Legal basis & references](#legal-basis--references)
- [Changelog](#changelog)
- [License](#license)

## Installation

```shell
dotnet add package Inflop.VatSharp
```

Or via `PackageReference`:

```xml
<PackageReference Include="Inflop.VatSharp" Version="*" />
```

## Quick start

```csharp
using Inflop.VatSharp;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.ValueObjects;

var engine = VatCalculationEngine.Create();

var items = new[]
{
    new InvoiceLineItem(UnitPrice.Net(5.48m), Quantity.Of(9), VatRate.Of(23)),
    new InvoiceLineItem(UnitPrice.Net(7.98m), Quantity.Of(2), VatRate.Of(23)),
    new InvoiceLineItem(UnitPrice.Net(1.99m), Quantity.Of(3), VatRate.Of(23)),
};

DocumentAmounts result = engine.Calculate(items, VatCalculationMethod.SumOfLineItemVatAmounts);

Console.WriteLine($"Net: {result.TotalNet}, VAT: {result.TotalVat}, Gross: {result.TotalGross}");
// Net: 71.25, VAT: 16.38, Gross: 87.63
```

## Fluent mapping engine

The mapping engine connects the library to **any** existing types. No interfaces to implement, no base classes to inherit. Configure once at startup, use everywhere.

```csharp
using Inflop.VatSharp;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Mapping;
using Inflop.VatSharp.ValueObjects;

// Your existing classes — unchanged:
public class Invoice { public List<LineItem> Lines { get; set; } }
public class LineItem { public decimal Price { get; set; } public int Qty { get; set; } public int Vat { get; set; } }

// Configure once:
var engine = VatCalculationEngine.For<Invoice, LineItem>(cfg => cfg
    .Document(doc => doc
        .LineItems(f => f.Lines)
        .Method(VatCalculationMethod.FromSumOfNetValues))
    .LineItem(line => line
        .NetUnitPrice(p => p.Price)
        .Quantity(p => p.Qty)
        .VatRate(p => p.Vat)));

// Use — one argument, engine knows everything:
DocumentAmounts result = engine.Calculate(myInvoice);
```

### Dynamic method selection

```csharp
.Method(f => f.Type switch
{
    "PAR" or "FP" => VatCalculationMethod.FromSumOfGrossValues,  // retail / advance
    _             => VatCalculationMethod.FromSumOfNetValues      // B2B
})
```

### Gross prices with tax code conversion

Method II (`FromSumOfGrossValues`) requires gross unit prices — use `.GrossUnitPrice(...)` instead of `.NetUnitPrice(...)`:

```csharp
.GrossUnitPrice(p => p.PreisBrutto)
.VatRate(p => p.Steuerkennzeichen switch
{
    "S" => VatRate.Of(19),   // DE standard
    "R" => VatRate.Of(7),    // DE reduced
    "F" => VatRate.Zero,     // zero-rated
    _   => throw new InvalidOperationException($"Unknown tax code: {p.Steuerkennzeichen}")
})
```

> **Method II constraint** — `FromSumOfGrossValues` requires **all** line items to carry a gross unit price. Passing net-priced items throws `VatCalculationException` at calculation time. This is intentional: the gross amount is the authoritative figure in retail/advance-payment scenarios, and mixing price types would produce legally incorrect results.

### Without document wrapper

```csharp
var engine = VatCalculationEngine.ForItems<MyLineDto>(line => line
    .NetUnitPrice(x => x.Price)
    .Quantity(x => x.Qty)
    .VatRate(x => x.VatPct));

DocumentAmounts result = engine.Calculate(myLines, VatCalculationMethod.FromSumOfNetValues);
```

### Single line item preview (e.g. UI editing)

```csharp
LineItemAmounts amounts = engine.CalculateLineItem(singleLine);
```

## Discounts

Art. 79 lit. b of Directive 2006/112/EC — discounts granted at the time of supply reduce the taxable amount. Discounts are applied to the line total (unit price × quantity) in the same price type as the unit price (net for net prices, gross for gross prices).

### Percentage discount

```csharp
var items = new[]
{
    new InvoiceLineItem(UnitPrice.Net(50.00m), Quantity.Of(3), VatRate.Of(23),
        Discount.OfPercentage(10m)),  // 10% off the line total
};

DocumentAmounts result = engine.Calculate(items, VatCalculationMethod.FromSumOfNetValues);
// 3 × 50.00 = 150.00 → 10% off = −15.00 → taxable net 135.00 → VAT 23%: 31.05
// Net: 135.00, VAT: 31.05, Gross: 166.05, Discount: 15.00
```

### Absolute discount (fixed amount)

```csharp
var items = new[]
{
    new InvoiceLineItem(UnitPrice.Net(120.00m), Quantity.Of(1), VatRate.Of(23),
        Discount.OfAmount(20.00m)),  // 20 currency units off the line
};

DocumentAmounts result = engine.Calculate(items, VatCalculationMethod.FromSumOfNetValues);
// 120.00 − 20.00 = 100.00 → VAT 23%: 23.00 → gross 123.00
```

### Discount via mapping engine

```csharp
var engine = VatCalculationEngine.ForItems<Line>(cfg => cfg
    .NetUnitPrice(x => x.Price)
    .Quantity(x => x.Qty)
    .VatRate(x => x.Rate)
    .DiscountPercentage(x => x.DiscountPct));   // decimal 0–100

// Absolute amount from a decimal field:
//     .DiscountAbsolute(x => x.DiscountAmt)     // decimal — always present
//     .DiscountAbsolute(x => x.DiscountAmt)     // decimal? — null means no discount

// Pre-built Discount value object:
//     .Discount(x => x.Disc)                    // Discount? — absolute or percentage
```

## Foreign currency

For invoices denominated in a foreign currency, the library produces amounts in both the invoice currency and the base (settlement) currency required for VAT declarations — e.g. PLN in Poland, EUR for euro-area countries. Legal basis: art. 91 of Directive 2006/112/EC.

### Creating an exchange rate

```csharp
// Factory method:
ExchangeRate eurPln = ExchangeRate.Of(
    CurrencyCode.EUR,
    CurrencyCode.PLN,
    4.2140m,
    new DateOnly(2026, 2, 25),   // publication date per art. 91
    "NBP");                       // source label — metadata only, no effect on calculations

// Fluent builder — identical result:
ExchangeRate eurPln2 = ExchangeRate.From(CurrencyCode.EUR)
    .To(CurrencyCode.PLN)
    .Rate(4.2140m)
    .Date(new DateOnly(2026, 2, 25))
    .Source("NBP");
```

`CurrencyCode` has named constants for common currencies (`CurrencyCode.EUR`, `.PLN`, `.USD`, `.GBP`, `.CHF`, `.CZK`, `.SEK`, `.NOK`, `.DKK`, `.HUF`) and a factory `CurrencyCode.Of("JPY")` for any ISO 4217 code.

### Exchange rate supplied by the caller

```csharp
var engine = VatCalculationEngine.ForItems<InvoiceLine>(cfg => cfg
    .NetUnitPrice(x => x.Price)
    .Quantity(x => x.Qty)
    .VatRate(x => x.Vat));

ExchangeRate eurPln = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.2140m,
    new DateOnly(2026, 2, 25), "NBP");

// Net 100.00 EUR @ 23% VAT, rate 4.2140 PLN/EUR
// EUR: net 100.00, VAT 23.00, gross 123.00
// PLN (FromSumOfNetValues): net = round(100.00 × 4.2140) = 421.40 → VAT = round(421.40 × 0.23) = 96.92
ForeignCurrencyDocumentAmounts result =
    engine.Calculate(lines, VatCalculationMethod.FromSumOfNetValues, eurPln);

// Net 100.00 EUR, VAT 23.00 EUR, Gross 123.00 EUR
// VAT for declaration: 96.92 PLN
```

### Exchange rate mapped from the document

```csharp
public class Invoice
{
    public List<InvoiceLine> Lines { get; set; }
    public ExchangeRate Rate { get; set; }   // stored on the document
}

var engine = VatCalculationEngine.For<Invoice, InvoiceLine>(cfg => cfg
    .Document(doc => doc
        .LineItems(f => f.Lines)
        .Method(VatCalculationMethod.FromSumOfNetValues)
        .ForeignCurrency(f => f.Rate))          // reads ExchangeRate from each document
    .LineItem(line => line
        .NetUnitPrice(p => p.Price)
        .Quantity(p => p.Qty)
        .VatRate(p => p.Vat)));

ForeignCurrencyDocumentAmounts result = engine.CalculateFcy(invoice);
```

### Constant rate for all documents (batch processing)

```csharp
// One central-bank rate applied to an entire monthly batch:
ExchangeRate monthlyRate = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.2140m,
    new DateOnly(2026, 1, 31), "NBP");

var engine = VatCalculationEngine.For<Invoice, InvoiceLine>(cfg => cfg
    .Document(doc => doc
        .LineItems(f => f.Lines)
        .Method(VatCalculationMethod.FromSumOfNetValues)
        .ForeignCurrency(monthlyRate))           // constant — no per-document accessor needed
    .LineItem(line => line
        .NetUnitPrice(p => p.Price)
        .Quantity(p => p.Qty)
        .VatRate(p => p.Vat)));

IEnumerable<ForeignCurrencyDocumentAmounts> results = invoices.Select(engine.CalculateFcy);
```

### Base-currency projection

```csharp
// Project FCY result to a standard DocumentAmounts in base currency only.
// Useful for periodic VAT returns or EC Sales Lists:
DocumentAmounts baseAmounts = result.ToBaseDocumentAmounts();
```

> **Note** — `ToBaseDocumentAmounts()` converts VAT rate summaries and document totals to the base currency, but `LineItems` intentionally **remain in the invoice (foreign) currency**. This is correct under Polish art. 106e ust. 11 ustawy o VAT and EU Directive 2006/112/EC art. 91: only the VAT amount must be declared in the settlement currency; there is no requirement to convert individual line items. Use `VatRateSummaries` and `TotalVatBase` for VAT declarations.

## Custom rounding

The library defaults to arithmetic rounding (2 decimal places, `MidpointRounding.AwayFromZero`) — the rule used by DE §14 UStG, NL art. 5a, and most EU member states. Two independent rounding strategies can be configured:

| Parameter              | Applies to                                                  | Default            |
|------------------------|-------------------------------------------------------------|--------------------|
| `Rounding`             | All invoice amounts (net, VAT, gross per line and per rate) | 2 dp, AwayFromZero |
| `BaseCurrencyRounding` | Base-currency amounts in FCY calculations only              | 2 dp, AwayFromZero |

### Invoice rounding

```csharp
using Inflop.VatSharp.Strategies.Rounding;

// HUF — no fractional currency unit (forint has no fillér in practice):
var engine = VatCalculationEngine.For<Doc, Line>(cfg => cfg
    .Rounding(DefaultRounding.ZeroDecimalPlaces)
    ...);

// CHF — 0.05 step rounding (Rappenausgleich for cash transactions):
public sealed class SwissRounding : IRoundingStrategy
{
    public decimal Round(decimal value) =>
        Math.Round(value / 0.05m, MidpointRounding.AwayFromZero) * 0.05m;
}

var engine = VatCalculationEngine.For<Doc, Line>(cfg => cfg
    .Rounding(new SwissRounding())
    ...);
```

### Base currency rounding for foreign-currency invoices

When your invoice is in one currency but VAT must be declared in another, the two amounts can require different precision:

```csharp
// EUR invoice, JPY base (0 decimal places — yen has no sub-unit):
// Net 100 EUR @ 23%, rate 161.47 JPY/EUR
// → VatBase = round(round(100 × 161.47, 0dp) × 0.23, 0dp) = 3 714 JPY
ExchangeRate eurJpy = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.Of("JPY"), 161.47m);

var engine = VatCalculationEngine.ForItems<Line>(
    cfg => cfg.NetUnitPrice(l => l.Price).Quantity(l => l.Qty).VatRate(l => l.Rate),
    baseCurrencyRounding: DefaultRounding.ZeroDecimalPlaces);

// EUR invoice, KWD base (3 decimal places — dinar is divided into 1000 fils):
ExchangeRate eurKwd = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.Of("KWD"), 0.3342m);

var kwdEngine = VatCalculationEngine.ForItems<Line>(
    cfg => cfg.NetUnitPrice(l => l.Price).Quantity(l => l.Qty).VatRate(l => l.Rate),
    baseCurrencyRounding: new DefaultRounding(3));
```

Both parameters can be set independently — a CHF-denominated invoice settled in JPY would use `Rounding(new SwissRounding())` together with `BaseCurrencyRounding(DefaultRounding.ZeroDecimalPlaces)`.

Via the fluent builder:

```csharp
var engine = VatCalculationEngine.For<Invoice, Line>(cfg => cfg
    .Document(d => d.LineItems(f => f.Lines).Method(f => f.CalcMethod))
    .LineItem(l => l.NetUnitPrice(f => f.Price).Quantity(f => f.Qty).VatRate(f => f.Rate))
    .BaseCurrencyRounding(DefaultRounding.ZeroDecimalPlaces));
```

## Dependency injection

The engine is immutable and thread-safe — register as a singleton:

```csharp
using Inflop.VatSharp.Mapping;

services.AddSingleton(VatCalculationEngine.For<Invoice, LineItem>(cfg => cfg
    .Document(d => d.LineItems(f => f.Lines).Method(VatCalculationMethod.FromSumOfNetValues))
    .LineItem(l => l.NetUnitPrice(p => p.Price).Quantity(p => p.Qty).VatRate(p => p.Vat))));

public class InvoiceService(VatCalculationEngine<Invoice, LineItem> engine)
{
    public DocumentAmounts Calculate(Invoice invoice) => engine.Calculate(invoice);
}
```

## Legal basis & references

### Calculation methods

| Method | EU Directive | Use case |
| ------ | ------------ | -------- |
| **I — From sum of net values** | art. 226 pts 8, 10 | B2B invoices |
| **II — From sum of gross values** | art. 226 | Retail, fiscal registers, advance payments |
| **III — Sum of line item VAT** | art. 226 pt 10 | Per-line VAT (may differ by ±1 unit from Method I) |

EU VAT Directive 2006/112/EC art. 226 specifies what must appear on an invoice (net amount, VAT rate, VAT amount payable) but does not mandate the arithmetic sequence for arriving at those amounts. Two ECJ judgments directly address the line-item vs. invoice-level distinction:

- **C-484/06 — Koninklijke Ahold** (10 July 2008) — VAT calculated on total receipt amount vs. per item. ECJ: Community law contains no obligation to permit per-item rounding; Member States decide, subject to fiscal neutrality and proportionality. [EUR-Lex](https://curia.europa.eu/juris/liste.jsf?language=en&num=c-484/06)
- **C-302/07 — J.D. Wetherspoon** (5 March 2009) — line-level rounding down vs. arithmetic rounding at basket level. ECJ: neither EU nor national law specifies whether rounding occurs at line or invoice level; arithmetic rounding ensures greater fiscal neutrality than always-down rounding. [EUR-Lex](https://eur-lex.europa.eu/legal-content/EN/TXT/?uri=CELEX:62007CJ0302)

### Related provisions

- **Art. 79 lit. b** — discounts granted at the time of supply reduce the taxable amount
- **Art. 91** — foreign currency conversion for VAT purposes

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for release notes. This project follows [Keep a Changelog](https://keepachangelog.com/) and [Semantic Versioning](https://semver.org/).

## Contributing

Bug reports and pull requests are welcome on [GitHub Issues](https://github.com/inflop/VatSharp/issues). For larger changes, please open an issue first to discuss the direction.

## License

Released under the [MIT License](LICENSE). Copyright © Inflop.
