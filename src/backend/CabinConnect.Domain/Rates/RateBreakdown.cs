namespace CabinConnect.Domain.Rates;

public sealed class RateBreakdown
{
    public IReadOnlyList<RateLineItem> LineItems { get; }
    public decimal TotalPrice { get; }

    public RateBreakdown(IReadOnlyList<RateLineItem> lineItems)
    {
        LineItems = lineItems;
        TotalPrice = lineItems.Sum(li => li.Rate);
    }
}
