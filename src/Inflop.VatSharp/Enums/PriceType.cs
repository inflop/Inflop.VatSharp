namespace Inflop.VatSharp.Enums;

/// <summary>
/// Discriminates between net and gross prices.
/// </summary>
public enum PriceType
{
    /// <summary>
    /// Price excluding VAT.
    /// </summary>
    Net = 0,
    
    /// <summary>
    /// Price including VAT.
    /// </summary>
    Gross = 1
}
