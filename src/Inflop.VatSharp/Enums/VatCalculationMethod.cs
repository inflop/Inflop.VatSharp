namespace Inflop.VatSharp.Enums;

/// <summary>
/// VAT calculation method used in EU VAT accounting under Directive 2006/112/EC.
/// The directive does not mandate a specific calculation method — these are accepted
/// accounting practices consistent with invoice data requirements of Art. 226.
/// </summary>
public enum VatCalculationMethod
{
    /// <summary>
    /// VAT = Sum(Net per rate) × rate.
    /// Standard B2B method. Produces the taxable amount per rate required by Art. 226(8)
    /// and the total VAT amount required by Art. 226(10). Not applicable to advance payments.
    /// </summary>
    FromSumOfNetValues = 0,

    /// <summary>
    /// VAT = Gross × rate / (100 + rate).
    /// Used when prices are quoted inclusive of VAT (retail, fiscal registers, advance payments).
    /// The gross consideration is the taxable amount per Art. 73 of Directive 2006/112/EC;
    /// VAT is derived algebraically from it. Use when net prices are unavailable.
    /// </summary>
    FromSumOfGrossValues = 1,

    /// <summary>
    /// Total VAT = sum of per-line VAT amounts, each rounded independently.
    /// Satisfies the VAT amount disclosure requirement of Art. 226(10) of Directive 2006/112/EC.
    /// May produce rounding differences compared to <see cref="FromSumOfNetValues"/>
    /// because rounding occurs at line level rather than at rate-group level —
    /// a legally valid outcome confirmed by ECJ C-484/06 (Ahold).
    /// </summary>
    SumOfLineItemVatAmounts = 2
}
