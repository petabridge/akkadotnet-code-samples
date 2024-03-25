using Akka.Actor;

namespace CqrsSqlServer.Shared.Queries;

public interface IFetchSoldProductsProtocol : ISqlShardingProtocolMember{ }

public class FetchSoldProducts
{
    private FetchSoldProducts(){}
    public static readonly FetchSoldProducts Instance = new();
}
/// <summary>
/// Query to the index actor to retrieve all sold products
/// </summary>
public sealed record FetchSoldProductsImpl(string ProducerId, IActorRef ConsumerController) : ISqlShardingProtocolMember;

public sealed class FetchSoldProductsResponse: IFetchSoldProductsProtocol
{
    public FetchSoldProductsResponse(IReadOnlyList<ProductsSoldData> products)
    {
        Products = products;
    }

    public IReadOnlyList<ProductsSoldData> Products { get; }
}