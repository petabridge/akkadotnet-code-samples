using Akka.Actor;

namespace CqrsSqlServer.Shared.Queries;

public interface IFetchAllProductsProtocol : ISqlShardingProtocolMember{ }

public sealed class FetchAllProducts
{
    private FetchAllProducts(){}
    public static readonly FetchAllProducts Instance = new();
}

/// <summary>
/// Query to the index actor to retrieve all products
/// </summary>
public sealed record FetchAllProductsImpl(string ProducerId, IActorRef ConsumerController) : ISqlShardingProtocolMember;


public sealed class FetchAllProductsResponse: IFetchAllProductsProtocol
{
    public FetchAllProductsResponse(IReadOnlyList<ProductData> products)
    {
        Products = products;
    }

    public IReadOnlyList<ProductData> Products { get; }
}