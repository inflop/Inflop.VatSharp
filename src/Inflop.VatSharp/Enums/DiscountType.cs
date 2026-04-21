namespace Inflop.VatSharp.Enums;

/// <summary>
/// Discriminates between the two legally recognized forms of price reduction.
/// Art. 79 lit. b of Directive 2006/112/EC covers both forms — both reduce
/// the taxable amount in the same way.
/// </summary>
public enum DiscountType
{
    /// <summary>
    /// Fixed monetary amount deducted from the line total.
    /// </summary>
    Absolute = 0,

    /// <summary>
    /// Percentage of the line total deducted.
    /// </summary>
    Percentage = 1
}
