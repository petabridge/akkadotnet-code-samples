namespace SqlSharding.Shared;

/// <summary>
/// Marker interface for all commands and events associated with a product.
/// </summary>
public interface IWithProductId
{
    string ProductId { get; }
}