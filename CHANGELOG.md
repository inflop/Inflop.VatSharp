# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## [1.0.0] - 2026-03-28

### Added

- Three EU VAT calculation methods (FromSumOfNetValues, FromSumOfGrossValues, SumOfLineItemVatAmounts)
- Fluent mapping engine for zero-friction integration with existing types
- Foreign currency support with exchange rate conversion (art. 91 Directive 2006/112/EC)
- Percentage and absolute discount support (art. 79 lit. b Directive 2006/112/EC)
- Configurable absolute discount behavior (FromTotal vs PerUnit)
- Value objects: Money, VatRate, Quantity, UnitPrice, CurrencyCode, Discount, ExchangeRate
- Custom rounding strategy support via IRoundingStrategy
- DI registration helpers (VatCalculationRegistration)
- Direct API via VatCalculationEngine.Create()
