namespace SqlSharding.Shared.Events;

/// <summary>
/// Used to distinguish product events from commands
/// </summary>
public interface IProductEvent : IWithProductId
{
}

public record ProductCreated(string ProductId, string ProductName, decimal Price) : IProductEvent;

public record ProductSold(ProductOrder Order, decimal UnitPrice, bool BackOrdered = false) : IProductEvent,
    IWithOrderId, IComparable<ProductSold>
{
    public string ProductId => Order.ProductId;

    public string OrderId => Order.OrderId;

    public decimal TotalPrice => Order.Quantity * UnitPrice;

    public int CompareTo(ProductSold? other)
    {
        if (other == null) return 1;

        return Order.Timestamp.CompareTo(other.Order.Timestamp);
    }
}

public enum InventoryChangeReason
{
    Fulfillment,
    SupplyIncrease,

    /// <summary>
    /// i.e. Theft or spoilage
    /// </summary>
    Lost
}

public record ProductInventoryChanged(string ProductId, int Quantity, DateTime Timestamp,
    InventoryChangeReason Reason = InventoryChangeReason.Fulfillment) : IProductEvent,
    IComparable<ProductInventoryChanged>
{
    public int CompareTo(ProductInventoryChanged? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return Timestamp.CompareTo(other.Timestamp);
    }
}

public enum ProductWarningReason
{
    /// <summary>
    /// Warning once inventory is running low.
    /// </summary>
    LowSupply,
    NoSupply
}

public record ProductInventoryWarningEvent(string ProductId, ProductWarningReason Reason, DateTime Timestamp,
    string Message) : IProductEvent, IComparable<ProductInventoryWarningEvent>
{
    public int CompareTo(ProductInventoryWarningEvent? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return ReferenceEquals(null, other) ? 1 : Timestamp.CompareTo(other.Timestamp);
    }
}