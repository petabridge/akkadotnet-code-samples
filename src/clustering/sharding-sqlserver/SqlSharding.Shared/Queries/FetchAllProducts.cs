using System.Collections.Immutable;

namespace SqlSharding.Shared.Queries;

/// <summary>
/// Query to the index actor to retrieve all products
/// </summary>
public sealed class FetchAllProducts : ISqlShardingProtocolMember
{
    public static readonly FetchAllProducts Instance = new();
    private FetchAllProducts(){}
}

public sealed class FetchAllProductsResponse: ISqlShardingProtocolMember
{
    public FetchAllProductsResponse(IReadOnlyList<ProductData> products)
    {
        Products = products;
    }

    public IReadOnlyList<ProductData> Products { get; }
}