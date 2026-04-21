namespace Inflop.VatSharp.Strategies.Rounding;

/// <summary>
/// Defines how monetary amounts are rounded during VAT calculations.
///
/// EU VAT Directive 2006/112/EC does not prescribe a specific rounding method.
/// Per ECJ C-302/07 (Wetherspoon), rounding rules are left to Member States.
///
/// The library ships with <see cref="DefaultRounding"/> (arithmetic, AwayFromZero).
/// Implement this interface for non-standard rules (e.g. CHF 0.05 step rounding).
/// </summary>
public interface IRoundingStrategy
{
    /// <summary>
    /// Rounds the specified monetary value according to the strategy's rules.
    /// </summary>
    /// <param name="value">The unrounded monetary value.</param>
    /// <returns>The rounded value.</returns>
    decimal Round(decimal value);
}
