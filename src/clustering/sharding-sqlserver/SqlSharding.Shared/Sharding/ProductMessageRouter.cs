using Akka.Cluster.Sharding;

namespace SqlSharding.Shared.Sharding;

public sealed class ProductMessageRouter : HashCodeMessageExtractor
{
    public ProductMessageRouter() : base(50) // use a default of 50 shards (5 ActorSystems hosting 10 a-piece)
    {
    }

    public override string? EntityId(object message)
    {
        if(message is IWithProductId productId)
        {
            return productId.ProductId;
        }

        return null;
    }
}