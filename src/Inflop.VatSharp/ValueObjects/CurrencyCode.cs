namespace Inflop.VatSharp.ValueObjects;

/// <summary>
/// ISO 4217 currency code (e.g. "EUR", "PLN", "USD").
/// </summary>
/// <remarks>
/// Intentionally implemented as <c>sealed record</c> (reference type) rather than
/// <c>readonly record struct</c>. A struct would expose an implicit parameterless
/// constructor, allowing <c>default(CurrencyCode)</c> to produce an instance with
/// <c>Value = null</c> — bypassing the non-empty-string invariant enforced in the
/// private constructor.
/// </remarks>
public sealed record CurrencyCode
{
    /// <summary>
    /// Euro (European Union).
    /// </summary>
    public static readonly CurrencyCode EUR = new("EUR");

    /// <summary>
    /// Polish zloty.
    /// </summary>
    public static readonly CurrencyCode PLN = new("PLN");

    /// <summary>
    /// United States dollar.
    /// </summary>
    public static readonly CurrencyCode USD = new("USD");

    /// <summary>
    /// British pound sterling.
    /// </summary>
    public static readonly CurrencyCode GBP = new("GBP");

    /// <summary>
    /// Swiss franc.
    /// </summary>
    public static readonly CurrencyCode CHF = new("CHF");

    /// <summary>
    /// Czech koruna.
    /// </summary>
    public static readonly CurrencyCode CZK = new("CZK");

    /// <summary>
    /// Swedish krona.
    /// </summary>
    public static readonly CurrencyCode SEK = new("SEK");

    /// <summary>
    /// Norwegian krone.
    /// </summary>
    public static readonly CurrencyCode NOK = new("NOK");

    /// <summary>
    /// Danish krone.
    /// </summary>
    public static readonly CurrencyCode DKK = new("DKK");

    /// <summary>
    /// Hungarian forint.
    /// </summary>
    public static readonly CurrencyCode HUF = new("HUF");

    /// <summary>
    /// Creates a currency code from a string. Normalized to upper case.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the code is null/empty or not exactly three letters.
    /// </exception>
    public static CurrencyCode Of(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new CurrencyCode(code.Trim().ToUpperInvariant());
    }

    /// <summary>
    /// The three-letter ISO 4217 code in upper case.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString()
        => Value;

    private CurrencyCode(string value)
        => Value = value.Length == 3 && value.All(char.IsLetter)
            ? value
            : throw new ArgumentException($"ISO 4217 currency code must be exactly 3 letters: '{value}'.", nameof(value));
}
