namespace SqlSharding.Shared.Events;

public record ProductCreated(string ProductId, string ProductName, decimal Price) : IWithProductId;

public record ProductSold(ProductOrder Order, decimal TotalCharged, bool BackOrdered = false) : IWithProductId, IWithOrderId
{
    public string ProductId => Order.ProductId;

    public string OrderId => Order.OrderId;
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

public record ProductInventoryChanged(string ProductId, int Quantity, InventoryChangeReason Reason = InventoryChangeReason.Fulfillment) : IWithProductId;

public enum ProductWarningReason
{
    /// <summary>
    /// Warning once inventory is running low.
    /// </summary>
    LowSupply,
    NoSupply
}

public record ProductInventoryWarningEvent(string ProductId, ProductWarningReason Reason, DateTime Timestamp) : IWithProductId;