using SqlSharding.Shared.Events;

namespace SqlSharding.Shared.Commands;

/// <summary>
/// Used to distinguish product events from commands
/// </summary>
public interface IProductCommand : IWithProductId{}

public record CreateProduct(string ProductId, string ProductName, decimal Price, int InitialQuantity) : IProductCommand;

public record SupplyProduct(string ProductId, int AdditionalQuantity) : IProductCommand;

public record PurchaseProduct(ProductOrder NewOrder) : IProductCommand, IWithOrderId
{
    public string ProductId => NewOrder.ProductId;
    public string OrderId => NewOrder.OrderId;
}

public record ProductCommandResponse(string ProductId, IReadOnlyCollection<IProductEvent> ResponseEvents, bool Success = true, string Message = "") : IWithProductId;