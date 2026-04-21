namespace Inflop.VatSharp.Exceptions;

/// <summary>
/// Exception thrown when an error occurs during VAT calculation,
/// e.g. due to invalid input data or an unexpected condition in the calculation logic.
/// </summary>
public sealed class VatCalculationException : Exception
{
    /// <inheritdoc />
    public VatCalculationException(string message) : base(message) { }

    /// <inheritdoc />
    public VatCalculationException(string message, Exception inner) : base(message, inner) { }
}
