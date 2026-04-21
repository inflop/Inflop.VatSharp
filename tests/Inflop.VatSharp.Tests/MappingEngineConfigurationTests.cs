using FluentAssertions;
using Inflop.VatSharp.Enums;
using Inflop.VatSharp.Exceptions;
using Inflop.VatSharp.Mapping;
using Inflop.VatSharp.ValueObjects;
using Xunit;

namespace Inflop.VatSharp.Tests;

// ═══════════════════════════════════════════════════════════════════════════
//  Fail-fast configuration validation — MappingConfigurationException
// ═══════════════════════════════════════════════════════════════════════════

public class MappingConfigurationTests
{
    private record Line(decimal Price, int Qty, int Vat);
    private record Doc(Line[] Lines);

    // ── LineItem mapping — required fields ────────────────────────────────

    [Fact]
    public void Build_MissingUnitPrice_ThrowsMappingConfigurationException()
    {
        // UnitPrice is required — without it the engine cannot determine
        // whether to apply net or gross calculation formulas.
        var act = () => VatCalculationEngine.For<Doc, Line>(cfg => cfg
            .Document(doc => doc.LineItems(f => f.Lines).Method(VatCalculationMethod.FromSumOfNetValues))
            .LineItem(line => line
                .Quantity(p => p.Qty)
                .VatRate(p => p.Vat)));   // UnitPrice deliberately omitted

        act.Should().Throw<MappingConfigurationException>()
            .WithMessage("*UnitPrice*");
    }

    [Fact]
    public void Build_MissingQuantity_ThrowsMappingConfigurationException()
    {
        var act = () => VatCalculationEngine.For<Doc, Line>(cfg => cfg
            .Document(doc => doc.LineItems(f => f.Lines).Method(VatCalculationMethod.FromSumOfNetValues))
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .VatRate(p => p.Vat)));   // Quantity deliberately omitted

        act.Should().Throw<MappingConfigurationException>()
            .WithMessage("*Quantity*");
    }

    [Fact]
    public void Build_MissingVatRate_ThrowsMappingConfigurationException()
    {
        var act = () => VatCalculationEngine.For<Doc, Line>(cfg => cfg
            .Document(doc => doc.LineItems(f => f.Lines).Method(VatCalculationMethod.FromSumOfNetValues))
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .Quantity(p => p.Qty)));   // VatRate deliberately omitted

        act.Should().Throw<MappingConfigurationException>()
            .WithMessage("*VatRate*");
    }

    // ── Document mapping — required fields ────────────────────────────────

    [Fact]
    public void Build_MissingLineItems_ThrowsMappingConfigurationException()
    {
        var act = () => VatCalculationEngine.For<Doc, Line>(cfg => cfg
            .Document(doc => doc.Method(VatCalculationMethod.FromSumOfNetValues))   // LineItems omitted
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .Quantity(p => p.Qty)
                .VatRate(p => p.Vat)));

        act.Should().Throw<MappingConfigurationException>()
            .WithMessage("*LineItems*");
    }

    [Fact]
    public void Build_MissingMethod_ThrowsMappingConfigurationException()
    {
        var act = () => VatCalculationEngine.For<Doc, Line>(cfg => cfg
            .Document(doc => doc.LineItems(f => f.Lines))   // Method omitted
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .Quantity(p => p.Qty)
                .VatRate(p => p.Vat)));

        act.Should().Throw<MappingConfigurationException>()
            .WithMessage("*Method*");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Runtime exception wrapping — MappingExecutionException
// ═══════════════════════════════════════════════════════════════════════════

public class MappingExecutionTests
{
    private record BadLine(decimal Price, int Qty, int Vat);
    private record BadDoc(BadLine[] Lines);

    [Fact]
    public void Calculate_LineItemsLambdaThrows_WrapsInMappingExecutionException()
    {
        // Simulates a DB/lazy-load failure in the LineItems accessor.
        var engine = VatCalculationEngine.For<BadDoc, BadLine>(cfg => cfg
            .Document(doc => doc
                .LineItems(_ => throw new InvalidOperationException("DB unavailable"))
                .Method(VatCalculationMethod.FromSumOfNetValues))
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .Quantity(p => p.Qty)
                .VatRate(p => p.Vat)));

        var doc = new BadDoc(Lines: []);
        var act = () => engine.Calculate(doc);

        act.Should().Throw<MappingExecutionException>()
            .WithMessage("*DB unavailable*");
    }

    [Fact]
    public void Calculate_MethodLambdaThrows_WrapsInMappingExecutionException()
    {
        // Simulates an unmapped document type in a dynamic method selector.
        var engine = VatCalculationEngine.For<BadDoc, BadLine>(cfg => cfg
            .Document(doc => doc
                .LineItems(f => f.Lines)
                .Method(_ => throw new InvalidOperationException("Unknown document type")))
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .Quantity(p => p.Qty)
                .VatRate(p => p.Vat)));

        var doc = new BadDoc(Lines: [new BadLine(10m, 1, 23)]);
        var act = () => engine.Calculate(doc);

        act.Should().Throw<MappingExecutionException>()
            .WithMessage("*Unknown document type*");
    }

    [Fact]
    public void Calculate_VatRateLambdaThrows_WrapsInMappingExecutionException()
    {
        // Simulates an unmapped tax code when converting from an external enum.
        var engine = VatCalculationEngine.For<BadDoc, BadLine>(cfg => cfg
            .Document(doc => doc
                .LineItems(f => f.Lines)
                .Method(VatCalculationMethod.FromSumOfNetValues))
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .Quantity(p => p.Qty)
                .VatRate((Func<BadLine, VatRate>)(_ => throw new InvalidOperationException("Unmapped tax code")))));

        var doc = new BadDoc(Lines: [new BadLine(10m, 1, 23)]);
        var act = () => engine.Calculate(doc);

        act.Should().Throw<MappingExecutionException>()
            .WithMessage("*Unmapped tax code*");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  VatCalculationRegistration — DI helper
// ═══════════════════════════════════════════════════════════════════════════

public class VatCalculationRegistrationTests
{
    private record Line(decimal Price, int Qty, int Vat);
    private record Doc(Line[] Lines);

    [Fact]
    public void CreateEngine_ReturnsCorrectType()
    {
        var engine = VatCalculationRegistration.CreateEngine<Doc, Line>(cfg => cfg
            .Document(doc => doc.LineItems(f => f.Lines).Method(VatCalculationMethod.FromSumOfNetValues))
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .Quantity(p => p.Qty)
                .VatRate(p => p.Vat)));

        engine.Should().BeOfType<VatCalculationEngine<Doc, Line>>();
    }

    [Fact]
    public void CreateEngine_ProducesCorrectCalculation()
    {
        // Result must be identical to calling VatCalculationEngine.For directly.
        var engine = VatCalculationRegistration.CreateEngine<Doc, Line>(cfg => cfg
            .Document(doc => doc.LineItems(f => f.Lines).Method(VatCalculationMethod.FromSumOfNetValues))
            .LineItem(line => line
                .NetUnitPrice(p => p.Price)
                .Quantity(p => p.Qty)
                .VatRate(p => p.Vat)));

        var result = engine.Calculate(new Doc(Lines: [new Line(100m, 1, 23)]));

        // Net 100 × 23% = 23 VAT → Gross 123
        result.TotalNet.Value.Should().Be(100m);
        result.TotalVat.Value.Should().Be(23m);
        result.TotalGross.Value.Should().Be(123m);
    }

    [Fact]
    public void CreateItemEngine_ReturnsCorrectType()
    {
        var engine = VatCalculationRegistration.CreateItemEngine<Line>(cfg => cfg
            .NetUnitPrice(p => p.Price)
            .Quantity(p => p.Qty)
            .VatRate(p => p.Vat));

        engine.Should().BeOfType<LineItemCalculationEngine<Line>>();
    }

    [Fact]
    public void CreateItemEngine_ProducesCorrectCalculation()
    {
        var engine = VatCalculationRegistration.CreateItemEngine<Line>(cfg => cfg
            .NetUnitPrice(p => p.Price)
            .Quantity(p => p.Qty)
            .VatRate(p => p.Vat));

        var result = engine.Calculate([new Line(50m, 2, 8)], VatCalculationMethod.FromSumOfNetValues);

        // 2 × 50 = 100 net × 8% = 8 VAT → Gross 108
        result.TotalNet.Value.Should().Be(100m);
        result.TotalVat.Value.Should().Be(8m);
        result.TotalGross.Value.Should().Be(108m);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Gross price mapping via GrossUnitPrice
// ═══════════════════════════════════════════════════════════════════════════

public class GrossPriceMappingTests
{
    private record GrossLine(decimal GrossPrice, int Qty, int Vat);
    private record GrossDoc(GrossLine[] Lines);

    [Fact]
    public void Engine_GrossUnitPrice_MappedCorrectly()
    {
        var engine = VatCalculationEngine.ForItems<GrossLine>(cfg => cfg
            .GrossUnitPrice(p => p.GrossPrice)
            .Quantity(p => p.Qty)
            .VatRate(p => p.Vat));

        var lines = new[]
        {
            new GrossLine(123.00m, 1, 23),  // Gross 123 → Net 100, VAT 23
        };

        var result = engine.Calculate(lines, VatCalculationMethod.FromSumOfGrossValues);

        var summary = result.VatRateSummaries.Single();
        summary.TotalGross.Value.Should().Be(123.00m);
        summary.TotalVat.Value.Should().Be(23.00m);
        summary.TotalNet.Value.Should().Be(100.00m);
    }

    [Fact]
    public void Engine_GrossUnitPrice_WithDocumentMapping()
    {
        var engine = VatCalculationEngine.For<GrossDoc, GrossLine>(cfg => cfg
            .Document(doc => doc
                .LineItems(f => f.Lines)
                .Method(VatCalculationMethod.FromSumOfGrossValues))
            .LineItem(line => line
                .GrossUnitPrice(p => p.GrossPrice)
                .Quantity(p => p.Qty)
                .VatRate(p => p.Vat)));

        var doc = new GrossDoc([new GrossLine(108.00m, 1, 8)]);

        var result = engine.Calculate(doc);

        result.TotalGross.Value.Should().Be(108.00m);
        result.TotalVat.Value.Should().Be(8.00m);
        result.TotalNet.Value.Should().Be(100.00m);
    }
}
