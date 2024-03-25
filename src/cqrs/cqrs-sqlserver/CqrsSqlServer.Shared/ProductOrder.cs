namespace CqrsSqlServer.Shared;

/// <summary>
/// Datatype used to encapsulate order values.
/// </summary>
public sealed record ProductOrder
    (string OrderId, string ProductId, int Quantity, DateTime Timestamp) : IWithProductId, IWithOrderId;