namespace CqrsSqlServer.Shared.Queries;

/// <summary>
/// Fetch a particular product
/// </summary>
public sealed class FetchProduct : IWithProductId
{
    public FetchProduct(string productId)
    {
        ProductId = productId;
    }

    public string ProductId { get; }
}

public sealed class FetchResult : ISqlShardingProtocolMember
{
    public FetchResult(ProductState state)
    {
        State = state;
    }

    public ProductState State { get; }
}