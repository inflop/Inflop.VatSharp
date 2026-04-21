using Inflop.VatSharp.Enums;

namespace Inflop.VatSharp.Strategies.Calculation;

/// <summary>
/// Resolves strategies. Stateless singletons.
/// </summary>
internal static class VatCalculationStrategyFactory
{
    private static readonly FromSumOfNetValuesStrategy Net = new();
    private static readonly FromSumOfGrossValuesStrategy Gross = new();
    private static readonly SumOfLineItemVatAmountsStrategy PerLine = new();

    /// <summary>
    /// Resolves the <see cref="IVatCalculationStrategy"/> for the given <paramref name="method"/>.
    /// </summary>
    public static IVatCalculationStrategy For(VatCalculationMethod method) => method switch
    {
        VatCalculationMethod.FromSumOfNetValues => Net,
        VatCalculationMethod.FromSumOfGrossValues => Gross,
        VatCalculationMethod.SumOfLineItemVatAmounts => PerLine,
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };
}