namespace SqlSharding.Shared.Commands;

public record CreateProduct(string ProductId, string ProductName, decimal Price, int InitialQuantity) : IWithProductId;

public record SupplyProduct(string ProductId, int AdditionalQuantity) : IWithProductId;

public record PurchaseProduct(ProductOrder NewOrder) : IWithProductId, IWithOrderId
{
    public string ProductId => NewOrder.ProductId;
    public string OrderId => NewOrder.OrderId;
}

public record ProductCommandResponse(string ProductId, IEnumerable<IWithProductId> ResponseEvents, bool Success = true) : IWithProductId;