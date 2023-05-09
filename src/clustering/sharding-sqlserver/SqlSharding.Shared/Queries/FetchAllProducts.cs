using System.Collections.Immutable;
using Akka.Actor;

namespace SqlSharding.Shared.Queries;

public interface IFetchAllProductsProtocol : ISqlShardingProtocolMember{ }

/// <summary>
/// Query to the index actor to retrieve all products
/// </summary>
public sealed record FetchAllProducts(string ProducerId, IActorRef ConsumerController) : ISqlShardingProtocolMember;
public sealed class FetchAllProductsResponse: IFetchAllProductsProtocol
{
    public FetchAllProductsResponse(IReadOnlyList<ProductData> products)
    {
        Products = products;
    }

    public IReadOnlyList<ProductData> Products { get; }
}