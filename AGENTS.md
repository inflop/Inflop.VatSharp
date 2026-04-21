# AGENTS.md

This file provides guidance to agentic coding assistants working with this repository.

## Build & Test Commands

```bash
# Build the solution
dotnet build src/Inflop.VatSharp.slnx

# Build a specific project
dotnet build src/Inflop.VatSharp/Inflop.VatSharp.csproj

# Run all tests
dotnet test src/Inflop.VatSharp.slnx

# Run a single test
dotnet test src/Inflop.VatSharp.slnx --filter "FullyQualifiedName~TestMethodName"

# Run tests in a specific class
dotnet test src/Inflop.VatSharp.slnx --filter "FullyQualifiedName~MoneyTests"
```

**Target framework**: .NET 10.0. Main library has zero NuGet dependencies. Tests use xUnit + FluentAssertions.

## Code Style Guidelines

### Types and Value Objects

- Value objects: `readonly record struct` for DDD primitives (Money, VatRate); `sealed record` when a parameterless constructor must be blocked (Quantity, CurrencyCode — see Key Conventions)
- Classes: `sealed` when not intended for inheritance, `record` when equality matters
- Private constructors with `internal static factory` or `public static Of()` for creation
- Properties: PascalCase with only getters for immutable types
- Private fields: camelCase with underscore prefix: `_rounding`, `_docMapping`
- Interfaces: `I` prefix, single purpose: `IVatCalculationStrategy`, `IRoundingStrategy`

### Naming Conventions

- Methods: PascalCase, verb-first for actions: `Calculate()`, `MapAll()`, `Validate()`
- Factory methods: `Of()` for value objects, `For()` for engine builders
- Static properties for well-known instances: `Money.Zero`, `VatRate.Zero`
- Exception classes: `sealed`, descriptive suffix: `VatCalculationException`, `MappingConfigurationException`

### Imports and Namespaces

- Full namespace imports sorted alphabetically
- Implicit usings enabled in .csproj (`<ImplicitUsings>enable</ImplicitUsings>`)
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- No `using static` except in test files

### Formatting and Structure

- XML documentation comments (`///`) on all public APIs
- No inline comments in code (unless explicitly requested)
- Expression-bodied members preferred: `=>` for single-statement methods
- Guard clauses at top of methods: `ArgumentNullException.ThrowIfNull(document)`
- One line for guards when simple: `ArgumentNullException.ThrowIfNull(lineItem);`

### Error Handling

- Validation: `ArgumentNullException.ThrowIfNull()` for nulls
- Range validation: `throw new ArgumentOutOfRangeException(nameof(param), "message")`
- Domain exceptions: custom sealed exceptions with message + inner exception
- Fail-fast on configuration errors (throw at Build time, not at runtime)

### Testing (xUnit + FluentAssertions)

- Test files named after tested type: `MoneyTests.cs`, `ValueObjectTests.cs`
- Fact for single-case tests, Theory for parameterized
- Arrange-Act-Assert with fluent syntax: `FluentActions.Invoking(() => Money.Of(-1m)).Should().Throw<ArgumentOutOfRangeException>()`
- One-line test methods when clear: `[Fact] public void Of_PositiveValue_Creates() => Money.Of(10.50m).Value.Should().Be(10.50m);`

### Architecture Patterns

- Strategy pattern: stateless, inject via interface, register in factory
- Builder pattern: fluent API, compile to immutable mapping objects
- Value objects: no behavior besides encapsulation + domain logic
- Internal classes hidden from public API where possible
- InternalsVisibleTo to test project for testing internal types

### DDD-Specific Conventions

- All public result types are immutable
- Factory methods over constructors for validation
- Static well-known instances (Zero) for common values
- Rich domain logic on value objects: `VatRate.VatFromNet()`, `Money.Round()`
- No currency handling in core; delegates to rounding strategy

### Project Structure

- Main library: `src/Inflop.VatSharp/` (zero dependencies)
- Tests: `tests/Inflop.VatSharp.Tests/` (xUnit + FluentAssertions)
- Value objects: `src/Inflop.VatSharp/ValueObjects/`
- Strategies: `src/Inflop.VatSharp/Strategies/Calculation/`, `Rounding/`, `Discount/`
- Exceptions: `src/Inflop.VatSharp/Exceptions/`
- Mapping engine: `src/Inflop.VatSharp/Mapping/`

## Architecture Overview

This is a C# library for EU VAT calculations compliant with Directive 2006/112/EC.

### Three Usage Paths

**1. Direct API** — already have `InvoiceLineItem` objects, no mapping needed:

```csharp
var engine = VatCalculationEngine.Create();
DocumentAmounts result = engine.Calculate(items, VatCalculationMethod.FromSumOfNetValues);
```

**2. Fluent Mapping Engine with document wrapper** — integrate with arbitrary existing types:

```csharp
var engine = VatCalculationEngine.For<TDoc, TLine>(cfg => cfg
    .Document(doc => doc.LineItems(f => f.Lines).Method(f => f.CalcMethod))
    .LineItem(line => line.NetUnitPrice(f => f.Price).Quantity(f => f.Qty).VatRate(f => f.Rate)));
DocumentAmounts result = engine.Calculate(myDocument);
```

**3. Fluent Mapping Engine without document wrapper** — line items only, no document type:

```csharp
var engine = VatCalculationEngine.ForItems<TLine>(cfg => cfg
    .NetUnitPrice(f => f.Price).Quantity(f => f.Qty).VatRate(f => f.Rate));
DocumentAmounts result = engine.Calculate(items, VatCalculationMethod.FromSumOfNetValues);
```

### Three Calculation Methods (Strategy Pattern)

Located in `src/Inflop.VatSharp/Strategies/Calculation/`:

| Method | Class | Legal Basis | Use Case |
| ------ | ----- | ----------- | -------- |
| `FromSumOfNetValues` | `FromSumOfNetValuesStrategy` | Art. 226 pts 8,10 | B2B invoices |
| `FromSumOfGrossValues` | `FromSumOfGrossValuesStrategy` | Art. 226 | Retail/fiscal |
| `SumOfLineItemVatAmounts` | `SumOfLineItemVatAmountsStrategy` | Art. 226 pt 10 | Per-line VAT |

All strategies group line items by VAT rate before calculating. New strategies implement `IVatCalculationStrategy` and register in `VatCalculationStrategyFactory`.

`IVatCalculationStrategy` has three responsibilities:

- `Calculate(lineItems, rounding, discountBehavior)` — domestic calculation
- `Calculate(lineItems, rounding)` — domestic with default discount behavior
- `BuildSummaryFcy(summary, exchangeRate, baseRounding)` — FCY base-currency conversion (each strategy uses its own authoritative field)

### Value Objects (DDD)

All in `src/Inflop.VatSharp/ValueObjects/` — immutable `readonly record struct` or `record`:

- `Money` — decimal amount, currency-agnostic; `Money.Zero`, `Money.Of(decimal)`, `Money.Raw(decimal)`
- `VatRate` — rate value with built-in formulas: `VatFromNet`, `VatFromGross`, `GrossFromNet`, `NetFromGross`; `VatRate.Zero`
- `UnitPrice` — `Money` tagged with `PriceType` (Net or Gross)
- `Quantity` — positive decimal quantity
- `Discount` — price reduction: `Discount.OfAmount(Money)` or `Discount.OfPercentage(decimal 0–100)`; see Discount section below
- `InvoiceLineItem` — input to calculation: `UnitPrice`, `Quantity`, `VatRate`, optional `Discount`
- `LineItemAmounts` — per-line calculation result
- `VatRateSummary` — totals grouped by VAT rate (Net, Vat, Gross, Discount)
- `DocumentAmounts` — full document result: `List<LineItemAmounts>` + `List<VatRateSummary>`, aggregated totals
- `CurrencyCode` — ISO 4217 currency code value object; `CurrencyCode.Of("EUR")`
- `ExchangeRate` — rate for FCY→base conversion; see Foreign Currency section below
- `ForeignCurrencyDocumentAmounts` — FCY result with amounts in both invoice and base currency
- `VatRateSummaryFcy` — per-rate FCY result with `TotalNet/Vat/Gross/Discount` (invoice currency) and `*Base` counterparts

### Rounding

`IRoundingStrategy` in `src/Inflop.VatSharp/Strategies/Rounding/`. Default is arithmetic rounding (2dp, MidpointRounding.AwayFromZero). Inject a custom implementation for non-standard rules (e.g., CHF 0.05).

### Discount System

Legal basis: art. 79 lit. b Directive 2006/112/EC — discounts reduce the taxable amount.

**`Discount` value object** (`src/Inflop.VatSharp/ValueObjects/Discount.cs`):

- `Discount.OfAmount(Money)` — fixed monetary deduction from line total
- `Discount.OfPercentage(decimal)` — percentage (0–100) of line total
- `Discount.CalculateFrom(Money baseAmount)` — computes monetary amount (unrounded)
- Applied to `InvoiceLineItem` optionally; zero discount by default

**`IAbsoluteDiscountBehavior`** controls how absolute discounts are applied to multi-unit lines (`src/Inflop.VatSharp/Strategies/Discount/`):

- `FromTotalAbsoluteDiscountBehavior` (default, singleton) — discount subtracted from line total: `unitPrice × qty − discount`
- `PerUnitAbsoluteDiscountBehavior` — discount applied per unit before multiplication: `(unitPrice − discount) × qty`

Inject a custom `IAbsoluteDiscountBehavior` via `VatCalculationEngine.Create(discountBehavior: ...)` or `ForItems(..., discountBehavior: ...)`.

### Foreign Currency (FCY)

For invoices denominated in a foreign currency. The library produces amounts in both the invoice currency and the base (settlement) currency required for VAT declarations (e.g. PLN for Poland, EUR for euro-area countries). Legal basis: Directive 2006/112/EC art. 91.

**`ExchangeRate`** (`src/Inflop.VatSharp/ValueObjects/ExchangeRate.cs`):

```csharp
// Direct factory
ExchangeRate rate = ExchangeRate.Of(CurrencyCode.Of("EUR"), CurrencyCode.Of("PLN"), 4.2140m, new DateOnly(2026, 2, 25), "NBP");

// Fluent builder
ExchangeRate rate = ExchangeRate.From(CurrencyCode.Of("EUR")).To(CurrencyCode.Of("PLN")).Rate(4.2140m).Date(new DateOnly(2026, 2, 25)).Source("NBP");
```

- `Source` is a free-form string label (e.g. "NBP", "ECB", "Bundesbank") — metadata only, no effect on calculations
- `RateDate` — the rate's publication/effective date (art. 91)
- `ConvertToBase(Money)` — converts unrounded; `ConvertToBase(Money, IRoundingStrategy)` — converts and rounds

**FCY calculation via the Mapping Engine** (`VatCalculationEngine<TDoc, TLine>`):

```csharp
// Rate embedded in document mapping:
ForeignCurrencyDocumentAmounts result = engine.CalculateFcy(document);

// Rate supplied by caller:
ForeignCurrencyDocumentAmounts result = engine.Calculate(document, exchangeRate);

// Rate + method override:
ForeignCurrencyDocumentAmounts result = engine.Calculate(document, VatCalculationMethod.FromSumOfNetValues, exchangeRate);
```

**`ForeignCurrencyDocumentAmounts`** contains:

- `TotalNet/Vat/Gross/Discount` — in invoice (foreign) currency
- `TotalNetBase/VatBase/GrossBase/DiscountBase` — in base (settlement) currency
- `VatRateSummaries` — `IReadOnlyList<VatRateSummaryFcy>` (per-rate, both currencies)
- `LineItems` — `IReadOnlyList<LineItemAmounts>` (foreign currency)
- `ToBaseDocumentAmounts()` — projects to `DocumentAmounts` where VAT rate summaries and totals are in base currency; `LineItems` intentionally remain in the invoice (foreign) currency — this is correct per Polish art. 106e ust. 11 ustawy o VAT and Directive art. 91 (only VAT totals must be declared in the settlement currency, not individual line items)

**FCY base-currency conversion is strategy-owned (OCP)**. Each strategy implements `BuildSummaryFcy` using its authoritative field:

- `FromSumOfNetValues`: `NetBase = conv(Net)` → `VatBase = VatFromNet(NetBase)` → `GrossBase = Net + Vat`
- `FromSumOfGrossValues`: `GrossBase = conv(Gross)` → `VatBase = VatFromGross(GrossBase)` → `NetBase = Gross − Vat`
- `SumOfLineItemVatAmounts`: `NetBase = conv(Net)`, `VatBase = conv(Vat)` → `GrossBase = Net + Vat`
- `DiscountBase = conv(Discount)` — independent of method in all three cases

### Mapping Engine Internals

`VatCalculationEngine` (static factory) → `VatCalculationEngineBuilder` → compiles to `DocumentMapping` + `LineItemMapping`. Errors in mapping configuration throw `MappingConfigurationException` at `Build()` time (fail-fast). Runtime mapping errors throw `MappingExecutionException`.

`ForeignCurrencyCalculator` is an internal orchestration class — not part of the public API. It delegates arithmetic to the same `IVatCalculationStrategy` instances used for domestic invoices, then applies `ExchangeRate` to convert each per-rate summary.

### DI Registration

```csharp
// Manual factory registration (works with any DI container):
services.AddSingleton(VatCalculationEngine.For<TDoc, TLine>(...));

// Via VatCalculationRegistration helper (useful when engine config is separate from DI registration):
services.AddSingleton(VatCalculationRegistration.CreateEngine<TDoc, TLine>(cfg => cfg
    .Document(d => d.LineItems(f => f.Lines).Method(f => f.CalcMethod))
    .LineItem(l => l.NetUnitPrice(f => f.Price).Quantity(f => f.Qty).VatRate(f => f.Rate))));
```

## Key Conventions

- All public result types are immutable; use `readonly record struct` for new value objects
- **Exception**: `Quantity` and `CurrencyCode` are `sealed record` (reference type), NOT `readonly record struct`. This is intentional — a struct would expose an implicit parameterless constructor allowing `default(T)` to bypass the positive-value / non-empty invariant. `static One` / `static PLN` returning `new(...)` instead of `static readonly` is accepted: `default(sealed record)` is `null`, not a zero-value instance.
- Validation uses `ArgumentNullException.ThrowIfNull()` and `ArgumentOutOfRangeException`
- Strategies are stateless — they can and should be singletons
- The library has no currency handling by design; rounding strategy controls precision
- `internal` access modifier is used aggressively to keep the public API minimal
- OCP: adding a new calculation method means implementing `IVatCalculationStrategy` (including `BuildSummaryFcy`) and registering in `VatCalculationStrategyFactory` — no `switch`/`if` on method type anywhere
