using Inflop.VatSharp.Strategies.Rounding;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Strategies.Discount;

/// <summary>
/// Default absolute discount behavior: subtracts the discount from the line total.
/// Formula: <c>unitPrice × quantity − discount</c>.
/// </summary>
public sealed class FromTotalAbsoluteDiscountBehavior : IAbsoluteDiscountBehavior
{
    /// <summary>
    /// Singleton instance for the default absolute discount behavior.
    /// </summary>
    public static readonly FromTotalAbsoluteDiscountBehavior Instance = new();

    /// <summary>
    /// Calculates the line total after applying an absolute discount, using the formula:
    /// <c>unitPrice × quantity − discount</c>.
    /// </summary>
    /// <param name="unitPrice">
    /// The price per unit, before discount. Must be non-negative.
    /// </param>
    /// <param name="quantity">
    /// The quantity of units. Must be non-negative.
    /// </param>
    /// <param name="absoluteDiscount">
    /// The absolute discount to apply. Must be non-negative.
    /// </param>
    /// <param name="rounding">
    /// Not used by this implementation — the result of <c>unitPrice × quantity − discount</c>
    /// is exact. The outer calculation pipeline rounds the returned value independently.
    /// </param>
    /// <returns>
    /// The total amount after applying the absolute discount.
    /// </returns>
    public Money TotalInInputType(Money unitPrice, Quantity quantity, Money absoluteDiscount, IRoundingStrategy rounding)
        => unitPrice * quantity - absoluteDiscount;

    private FromTotalAbsoluteDiscountBehavior()
    {
    }
}
