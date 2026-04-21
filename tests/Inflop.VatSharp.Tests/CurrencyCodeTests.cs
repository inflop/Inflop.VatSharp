using FluentAssertions;
using Inflop.VatSharp.ValueObjects;
using Xunit;

namespace Inflop.VatSharp.Tests;

public class CurrencyCodeTests
{
    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public void Of_ValidCode_Creates(string code)
        => CurrencyCode.Of(code).Value.Should().Be(code);

    [Theory]
    [InlineData("eur")]
    [InlineData("Eur")]
    public void Of_LowerCase_NormalisedToUpper(string code)
        => CurrencyCode.Of(code).Value.Should().Be("EUR");

    [Theory]
    [InlineData("PL")]
    [InlineData("EURO")]
    [InlineData("12E")]
    public void Of_Invalid_Throws(string code)
        => FluentActions.Invoking(() => CurrencyCode.Of(code))
            .Should().Throw<ArgumentException>();

    [Fact]
    public void Of_Empty_Throws()
        => FluentActions.Invoking(() => CurrencyCode.Of(""))
            .Should().Throw<ArgumentException>();

    [Fact]
    public void WellKnown_HaveCorrectValues()
    {
        CurrencyCode.PLN.Value.Should().Be("PLN");
        CurrencyCode.EUR.Value.Should().Be("EUR");
        CurrencyCode.USD.Value.Should().Be("USD");
    }

    [Fact]
    public void Equality_SameCode_Equal()
        => CurrencyCode.Of("EUR").Should().Be(CurrencyCode.EUR);
}
