using System;
using FluentAssertions;
using Inflop.VatSharp.Strategies.Rounding;
using Inflop.VatSharp.ValueObjects;
using Xunit;

namespace Inflop.VatSharp.Tests;

public class MoneyTests
{
    [Fact]
    public void Of_PositiveValue_Creates() => Money.Of(10.50m).Value.Should().Be(10.50m);

    [Fact]
    public void Of_Zero_Creates() => Money.Of(0m).Value.Should().Be(0m);

    [Fact]
    public void Of_Negative_Throws() =>
        FluentActions.Invoking(() => Money.Of(-1m)).Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void Addition() => (Money.Of(10m) + Money.Of(3.50m)).Value.Should().Be(13.50m);

    [Fact]
    public void Subtraction() => (Money.Of(10m) - Money.Of(3m)).Value.Should().Be(7m);

    [Fact]
    public void MultiplyByQuantity() =>
        (Money.Of(4.58m) * Quantity.Of(4)).Value.Should().Be(18.32m);

    [Fact]
    public void Round_AwayFromZero() =>
        Money.Raw(12.345m).Round(DefaultRounding.TwoDecimalPlaces).Value.Should().Be(12.35m);

    [Fact]
    public void Round_BelowHalf_Down() =>
        Money.Raw(12.344m).Round(DefaultRounding.TwoDecimalPlaces).Value.Should().Be(12.34m);
    [Fact]
    public void Subtraction_ProducingNegativeResult_Throws() =>
        FluentActions.Invoking(() => { var _ = Money.Of(3m) - Money.Of(10m); })
            .Should().Throw<ArgumentOutOfRangeException>();
}

public class QuantityTests
{
    [Fact]
    public void Of_Positive_Creates() => Quantity.Of(3m).Value.Should().Be(3m);

    [Fact]
    public void Of_Zero_Throws() =>
        FluentActions.Invoking(() => Quantity.Of(0m)).Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void Of_Negative_Throws() =>
        FluentActions.Invoking(() => Quantity.Of(-1m)).Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void Of_Decimal_Creates() => Quantity.Of(1.5m).Value.Should().Be(1.5m);
}

public class VatRateTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(23)]
    [InlineData(27)]
    [InlineData(100)]
    public void Of_ValidPercentage_Creates(int pct) => VatRate.Of(pct).Percentage.Should().Be(pct);

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Of_OutOfRange_Throws(int pct) =>
        FluentActions.Invoking(() => VatRate.Of(pct)).Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void VatFromNet_23Percent()
    {
        var vat = VatRate.Of(23).VatFromNet(Money.Of(100m));
        vat.Value.Should().Be(23m);
    }

    [Fact]
    public void VatFromGross_23Percent()
    {
        // (123 × 23) / (100 + 23) = 2829 / 123 = 23.0
        var vat = VatRate.Of(23).VatFromGross(Money.Of(123m));
        vat.Value.Should().Be(23m);
    }

    [Fact]
    public void VatFromGross_ZeroRate_ReturnsZero()
    {
        var vat = VatRate.Zero.VatFromGross(Money.Of(100m));
        vat.Value.Should().Be(0m);
    }

    [Fact]
    public void SuperReducedRate_2point1_Works()
    {
        var rate = VatRate.Of(2.1m);
        rate.Percentage.Should().Be(2.1m);
        rate.Multiplier.Should().Be(0.021m);
    }

    [Fact]
    public void GrossFromNet_23Percent_ReturnsNetPlusVat()
    {
        // gross = net + VAT(net) = 100 + 100 × 23% = 123.00 (unrounded)
        var gross = VatRate.Of(23).GrossFromNet(Money.Of(100m));
        gross.Value.Should().Be(123m);
    }

    [Fact]
    public void GrossFromNet_ZeroRate_ReturnsNet()
    {
        // 0% rate — no VAT, gross = net
        var gross = VatRate.Zero.GrossFromNet(Money.Of(100m));
        gross.Value.Should().Be(100m);
    }

    [Fact]
    public void NetFromGross_23Percent_ReturnsGrossMinusVat()
    {
        // gross = 123 → VAT = 123 × 23/123 = 23 → net = 100 (exact, no rounding needed)
        var net = VatRate.Of(23).NetFromGross(Money.Of(123m));
        net.Value.Should().Be(100m);
    }

    [Fact]
    public void NetFromGross_8Percent_ReturnsCorrectNet()
    {
        // gross = 108 → VAT = 108 × 8/108 = 8 → net = 100 (exact)
        var net = VatRate.Of(8).NetFromGross(Money.Of(108m));
        net.Value.Should().Be(100m);
    }

    [Fact]
    public void NetFromGross_ZeroRate_ReturnsGross()
    {
        var net = VatRate.Zero.NetFromGross(Money.Of(100m));
        net.Value.Should().Be(100m);
    }

    [Fact]
    public void GrossFromNet_AndNetFromGross_AreInverse()
    {
        // Round-trip: NetFromGross(GrossFromNet(x)) = x for exact values
        var rate = VatRate.Of(23);
        var original = Money.Of(100m);
        var gross = rate.GrossFromNet(original);
        var net = rate.NetFromGross(gross);
        net.Value.Should().Be(original.Value);
    }

    // ── Symbol property and equality semantics ──────────────────────────────

    [Fact]
    public void Of_WithSymbol_SetsSymbol()
    {
        // Explicit symbol overload preserves the supplied label verbatim
        VatRate.Of(0m, "ZW").Symbol.Should().Be("ZW");
    }

    [Theory]
    [InlineData(23, "23%")]
    [InlineData(8, "8%")]
    [InlineData(0, "0%")]
    public void Of_WithoutSymbol_DefaultsToPercentageString(int percentage, string expectedSymbol)
    {
        // Default symbol = invariant-culture "{percentage}%" formatting
        VatRate.Of((decimal)percentage).Symbol.Should().Be(expectedSymbol);
    }

    [Fact]
    public void Of_WithoutSymbol_DefaultsToInvariantDecimalPercentageString()
    {
        // 5.5m formatted invariant-culture is "5.5%" (decimal point, never comma)
        VatRate.Of(5.5m).Symbol.Should().Be("5.5%");
    }

    [Fact]
    public void Of_Zero_HasZeroPercentSymbol()
    {
        // VatRate.Zero is defined as new(0m, "0%")
        VatRate.Zero.Symbol.Should().Be("0%");
    }

    [Fact]
    public void Of_ZW_NotEqualTo_NP()
    {
        // Both have Percentage=0 but different Symbol → distinct value-object identity
        // → separate VatRateSummary rows in JPK_V7 / art. 226 pts 8–10 reporting
        var zw = VatRate.Of(0m, "ZW");
        var np = VatRate.Of(0m, "NP");
        zw.Should().NotBe(np);
        (zw == np).Should().BeFalse();
    }

    [Fact]
    public void Of_ZW_NotEqualTo_DefaultZero()
    {
        // ZW (exempt, art. 43 ustawy o VAT) is legally distinct from 0% (zero-rated, art. 83)
        // Even though Percentage matches (=0), Symbol differs → not equal
        VatRate.Of(0m, "ZW").Should().NotBe(VatRate.Of(0m));
    }

    [Fact]
    public void Of_SamePercentageAndSymbol_AreEqual()
    {
        // Structural equality: same Percentage AND same Symbol → equal
        var a = VatRate.Of(0m, "ZW");
        var b = VatRate.Of(0m, "ZW");
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Of_ZeroOf_EqualsVatRateZero()
    {
        // VatRate.Of(0m) defaults Symbol to "0%" — same as VatRate.Zero
        VatRate.Of(0m).Should().Be(VatRate.Zero);
        (VatRate.Of(0m) == VatRate.Zero).Should().BeTrue();
    }

    [Fact]
    public void Of_IntWithoutSymbol_DefaultsToPercentageString()
    {
        // int overload delegates to Of(decimal) → same default symbol formatting
        VatRate.Of(23).Symbol.Should().Be("23%");
    }

    [Fact]
    public void ToString_ReturnsSymbol()
    {
        // ToString() returns Symbol verbatim (not "{Percentage}%" anymore)
        VatRate.Of(0m, "ZW").ToString().Should().Be("ZW");
        VatRate.Of(23).ToString().Should().Be("23%");
    }

    [Theory]
    [InlineData("ZW")]
    [InlineData("NP")]
    [InlineData("0%")]
    public void IsZero_ZeroPercentage_RegardlessOfSymbol(string symbol)
    {
        // IsZero is driven solely by Percentage == 0m; Symbol is irrelevant
        VatRate.Of(0m, symbol).IsZero.Should().BeTrue();
    }

    [Fact]
    public void CompareTo_SamePercentage_OrdersBySymbolAlphabetically()
    {
        // When Percentage ties, ordinal Symbol compare breaks the tie: 'N' < 'Z' → < 0
        VatRate.Of(0m, "NP").CompareTo(VatRate.Of(0m, "ZW")).Should().BeLessThan(0);
        VatRate.Of(0m, "ZW").CompareTo(VatRate.Of(0m, "NP")).Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_DifferentPercentage_OrdersByPercentageFirst()
    {
        // Primary key is Percentage: 0% < 23% even when Symbol of 0% sorts later alphabetically
        VatRate.Of(0m, "ZW").CompareTo(VatRate.Of(23)).Should().BeLessThan(0);
    }

    [Fact]
    public void Of_NullSymbol_Throws()
    {
        // ArgumentException.ThrowIfNullOrWhiteSpace(symbol) surfaces null as ArgumentException
        FluentActions.Invoking(() => VatRate.Of(0m, null!))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Of_WhitespaceSymbol_Throws(string symbol)
    {
        // Whitespace / empty symbols are rejected — invoice symbol must be a meaningful label
        FluentActions.Invoking(() => VatRate.Of(0m, symbol))
            .Should().Throw<ArgumentException>();
    }
}

public class DefaultRoundingTests
{
    [Theory]
    [InlineData(12.5166, 12.52)]  // 54.42 × 23% → article example
    [InlineData(2.0384, 2.04)]    // 25.48 × 8%  → article example
    [InlineData(4.2136, 4.21)]    // 18.32 × 23% → article example
    [InlineData(8.303, 8.30)]     // 36.10 × 23% → article example
    [InlineData(0.005, 0.01)]     // midpoint → up (AwayFromZero)
    [InlineData(0.004, 0.00)]     // below midpoint → down
    [InlineData(10.0, 10.0)]      // exact → unchanged
    public void TwoDecimalPlaces_Rounds_AwayFromZero(decimal input, decimal expected) =>
        DefaultRounding.TwoDecimalPlaces.Round(input).Should().Be(expected);

    [Theory]
    [InlineData(12.5, 13)]
    [InlineData(12.4, 12)]
    public void ZeroDecimalPlaces_Rounds_WholeUnits(decimal input, decimal expected) =>
        DefaultRounding.ZeroDecimalPlaces.Round(input).Should().Be(expected);
}

public class UnitPriceTests
{
    [Fact]
    public void Net_CreatesNetPrice() =>
        UnitPrice.Net(10m).IsNet.Should().BeTrue();

    [Fact]
    public void Gross_CreatesGrossPrice() =>
        UnitPrice.Gross(12.30m).IsGross.Should().BeTrue();
}