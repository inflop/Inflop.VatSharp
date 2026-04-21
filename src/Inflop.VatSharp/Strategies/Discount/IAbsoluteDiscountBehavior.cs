using Inflop.VatSharp.Strategies.Rounding;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Strategies.Discount;

/// <summary>
/// Determines how an absolute monetary discount is applied to a line item.
///
/// The discount reduces the taxable base per art. 79 lit. b of Directive 2006/112/EC.
/// This interface controls whether the discount is subtracted from the line total
/// or distributed per unit before multiplication.
///
/// Built-in implementations: <see cref="FromTotalAbsoluteDiscountBehavior"/> (default)
/// and <see cref="PerUnitAbsoluteDiscountBehavior"/>.
/// </summary>
public interface IAbsoluteDiscountBehavior
{
    /// <summary>
    /// Computes the line total (in the item's input price type) after applying an absolute discount.
    /// The <paramref name="rounding"/> parameter is available for implementations that require
    /// intermediate rounding (e.g. per-unit discount distribution). The default implementation
    /// <see cref="FromTotalAbsoluteDiscountBehavior"/> does not round — its result is exact.
    /// The outer calculation pipeline rounds the returned value independently.
    /// </summary>
    Money TotalInInputType(Money unitPrice, Quantity quantity, Money absoluteDiscount, IRoundingStrategy rounding);
}
