using FluentAssertions;
using Inflop.VatSharp.Strategies.Rounding;
using Inflop.VatSharp.ValueObjects;
using Xunit;

namespace Inflop.VatSharp.Tests;

public class ExchangeRateTests
{
    private static readonly DateOnly TestDate = new(2024, 10, 21);

    [Fact]
    public void Of_SetsAllFieldsExplicitly()
    {
        var rate = ExchangeRate.Of(CurrencyCode.USD, CurrencyCode.EUR, 0.92m, TestDate, "ECB");

        rate.ForeignCurrency.Should().Be(CurrencyCode.USD);
        rate.BaseCurrency.Should().Be(CurrencyCode.EUR);
        rate.Rate.Should().Be(0.92m);
        rate.Source.Should().Be("ECB");
        rate.RateDate.Should().Be(TestDate);
    }

    [Fact]
    public void Of_SetsBaseCurrencyExplicitly()
    {
        ExchangeRate.Of(CurrencyCode.USD, CurrencyCode.PLN, 3.98m, TestDate).BaseCurrency.Should().Be(CurrencyCode.PLN);
        ExchangeRate.Of(CurrencyCode.USD, CurrencyCode.EUR, 0.92m, TestDate).BaseCurrency.Should().Be(CurrencyCode.EUR);
    }

    [Fact]
    public void Of_SameCurrencyForBothArguments_Throws()
        => FluentActions.Invoking(() => ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.EUR, 1m, TestDate))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*EUR*");

    [Fact]
    public void Of_SameCurrencyAsBaseCurrency_Throws()
        => FluentActions.Invoking(() => ExchangeRate.Of(CurrencyCode.PLN, CurrencyCode.PLN, 1m, TestDate))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*PLN*");

    [Fact]
    public void Of_ZeroRate_Throws()
        => FluentActions.Invoking(() => ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 0m, TestDate))
            .Should()
            .Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void Of_NegativeRate_Throws()
        => FluentActions.Invoking(() => ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, -1m, TestDate))
            .Should()
            .Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void Of_WithoutRateDate_RateDateIsNull()
    {
        var rate = ExchangeRate.Of(CurrencyCode.USD, CurrencyCode.EUR, 0.92m, source: "ECB");
        rate.RateDate.Should().BeNull();
    }

    [Fact]
    public void Of_WithoutSourceAndDate_SourceNullAndDateNull()
    {
        var rate = ExchangeRate.Of(CurrencyCode.USD, CurrencyCode.EUR, 0.92m);
        rate.Source.Should().BeNull();
        rate.RateDate.Should().BeNull();
    }

    [Fact]
    public void Of_GbpToEur_AnySourceLabel()
    {
        var rate = ExchangeRate.Of(CurrencyCode.GBP, CurrencyCode.EUR, 1.17m, TestDate, "Bank of England");

        rate.Source.Should().Be("Bank of England");
        rate.ForeignCurrency.Should().Be(CurrencyCode.GBP);
        rate.BaseCurrency.Should().Be(CurrencyCode.EUR);
    }

    [Fact]
    public void ConvertToBase_RoundsToTwoDecimalPlaces()
    {
        // 287.50 EUR × 4.2345 = 1217.4187... → 1217.42
        var rate = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.2345m, TestDate, "NBP");
        var vat = Money.Of(287.50m);

        rate.ConvertToBase(vat, DefaultRounding.TwoDecimalPlaces).Value.Should().Be(1217.42m);
    }

    [Fact]
    public void ConvertToBase_UnroundedVariant_ReturnsRaw()
    {
        // 287.50 × 4.2345 = 1217.41875 (unrounded)
        var rate = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.2345m, TestDate);
        rate.ConvertToBase(Money.Of(287.50m)).Value.Should().Be(1217.41875m);
    }

    [Fact]
    public void Default_Rate_ThrowsNullReferenceException()
        => FluentActions.Invoking(() => default(ExchangeRate)!.Rate)
            .Should().Throw<NullReferenceException>();

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_SourceAndDate_ContainsAllKeyInfo()
    {
        var str = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.2345m, TestDate, "NBP").ToString();
        str.Should().Contain("EUR").And.Contain("4.2345").And.Contain("NBP").And.Contain("2024-10-21").And.Contain("PLN");
    }

    [Fact]
    public void ToString_NonPlnBase_ContainsBaseCurrency()
    {
        var rate = ExchangeRate.Of(CurrencyCode.USD, CurrencyCode.EUR, 0.92m, TestDate, "ECB");
        rate.ToString().Should().Contain("USD").And.Contain("EUR").And.Contain("ECB");
    }

    [Fact]
    public void ToString_WhenSourceIsNull_OmitsSourceSegment()
    {
        var rate = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.21m, TestDate);
        rate.ToString().Should().Contain("EUR").And.Contain("PLN").And.Contain("2024-10-21").And.NotContain("(,");
    }

    [Fact]
    public void ToString_WhenRateDateIsNull_OmitsDateSegment()
    {
        ExchangeRate rate = ExchangeRate.From(CurrencyCode.EUR).To(CurrencyCode.PLN).Rate(4.21m);
        rate.ToString().Should().Contain("EUR").And.Contain("PLN").And.NotContain("0001");
    }

    [Fact]
    public void ToString_WhenSourceAndDateNull_NoParentheses()
    {
        var rate = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.21m);
        rate.ToString().Should().Be("1 EUR = 4.2100 PLN");
    }

    [Fact]
    public void ToString_SourceOnlyNoDate_ContainsSourceInParentheses()
    {
        var rate = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.21m, source: "ECB");
        rate.ToString().Should().Be("1 EUR = 4.2100 PLN (ECB)");
    }

    [Fact]
    public void ToString_DateOnlyNoSource_ContainsDateInParentheses()
    {
        var rate = ExchangeRate.Of(CurrencyCode.EUR, CurrencyCode.PLN, 4.21m, TestDate);
        rate.ToString().Should().Be("1 EUR = 4.2100 PLN (2024-10-21)");
    }

    // ── Fluent builder ────────────────────────────────────────────────────────

    [Fact]
    public void Default_IsNull_CannotBeUsedAsInvalidExchangeRate()
    {
        ExchangeRate? rate = default(ExchangeRate);
        rate.Should().BeNull();
    }

    [Fact]
    public void FluentBuilder_AllSteps_ProducesCorrectExchangeRate()
    {
        ExchangeRate rate = ExchangeRate
            .From(CurrencyCode.EUR)
            .To(CurrencyCode.PLN)
            .Rate(4.2140m)
            .Date(TestDate)
            .Source("NBP");

        rate.ForeignCurrency.Should().Be(CurrencyCode.EUR);
        rate.BaseCurrency.Should().Be(CurrencyCode.PLN);
        rate.Rate.Should().Be(4.2140m);
        rate.RateDate.Should().Be(TestDate);
        rate.Source.Should().Be("NBP");
    }

    [Fact]
    public void FluentBuilder_WithoutDate_RateDateIsNull()
    {
        ExchangeRate rate = ExchangeRate.From(CurrencyCode.USD).To(CurrencyCode.EUR).Rate(0.92m);
        rate.RateDate.Should().BeNull();
    }

    [Fact]
    public void FluentBuilder_WithoutSource_SourceIsNull()
    {
        ExchangeRate rate = ExchangeRate.From(CurrencyCode.GBP).To(CurrencyCode.PLN).Rate(5.10m);
        rate.Source.Should().BeNull();
    }

    [Fact]
    public void FluentBuilder_DateBeforeSource_SameResultAsSourceBeforeDate()
    {
        ExchangeRate dateFirst = ExchangeRate.From(CurrencyCode.EUR).To(CurrencyCode.PLN)
            .Rate(4.21m).Date(TestDate).Source("ECB");

        ExchangeRate sourceFirst = ExchangeRate.From(CurrencyCode.EUR).To(CurrencyCode.PLN)
            .Rate(4.21m).Source("ECB").Date(TestDate);

        dateFirst.Should().Be(sourceFirst);
    }

    [Fact]
    public void FluentBuilder_SameCurrency_ThrowsAtToStep()
        => FluentActions.Invoking(() => ExchangeRate.From(CurrencyCode.EUR).To(CurrencyCode.EUR))
            .Should().Throw<ArgumentException>().WithMessage("*EUR*");

    [Fact]
    public void FluentBuilder_ZeroRate_ThrowsAtRateStep()
        => FluentActions.Invoking(() => ExchangeRate.From(CurrencyCode.EUR).To(CurrencyCode.PLN).Rate(0m))
            .Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void FluentBuilder_NegativeRate_ThrowsAtRateStep()
        => FluentActions.Invoking(() => ExchangeRate.From(CurrencyCode.EUR).To(CurrencyCode.PLN).Rate(-1m))
            .Should().Throw<ArgumentOutOfRangeException>();
}
