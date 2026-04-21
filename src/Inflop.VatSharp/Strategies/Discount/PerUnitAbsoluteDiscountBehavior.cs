using Inflop.VatSharp.Strategies.Rounding;
using Inflop.VatSharp.ValueObjects;

namespace Inflop.VatSharp.Strategies.Discount;

/// <summary>
/// Per-unit absolute discount behavior: distributes the discount per unit, rounds, then multiplies.
/// Formula: <c>(unitPrice − round(discount / quantity)) × quantity</c>.
/// When rounding causes <c>discountPerUnit &gt; unitPrice</c> (e.g. a very small quantity
/// with a large absolute discount), the effective unit price is clamped to zero rather than throwing.
/// In that case the full pre-discount line total is absorbed by the discount.
/// </summary>
public sealed class PerUnitAbsoluteDiscountBehavior : IAbsoluteDiscountBehavior
{
    /// <summary>
    /// Singleton instance for the per-unit absolute discount behavior.
    /// </summary>
    public static readonly PerUnitAbsoluteDiscountBehavior Instance = new();

    /// <summary>
    /// Calculates the line total after applying an absolute discount, using the formula:
    /// <c>(unitPrice − round(discount / quantity)) × quantity</c>.
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
    /// The rounding strategy used to round the intermediate <c>discount / quantity</c>
    /// value to the per-unit discount amount. Not applied to the final total.
    /// </param>
    /// <returns>
    /// The total amount after applying the absolute discount.
    /// </returns>
    public Money TotalInInputType(Money unitPrice, Quantity quantity, Money absoluteDiscount, IRoundingStrategy rounding)
    {
        var discountPerUnit = Money.Raw(absoluteDiscount.Value / quantity.Value).Round(rounding);
        var effectiveUnitPrice = discountPerUnit > unitPrice ? Money.Zero : unitPrice - discountPerUnit;
        return effectiveUnitPrice * quantity;
    }

    private PerUnitAbsoluteDiscountBehavior()
    {
    }
}
