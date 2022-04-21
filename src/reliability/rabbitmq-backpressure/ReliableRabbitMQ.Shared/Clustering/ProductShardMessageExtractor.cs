using Akka.Cluster.Sharding;
using ReliableRabbitMQ.Shared.Messages;

namespace ReliableRabbitMQ.Shared.Clustering;

public sealed class ProductShardMessageExtractor : HashCodeMessageExtractor
{
    public ProductShardMessageExtractor() : base(50)
    {
    }

    public override string EntityId(object message)
    {
        if (message is IProductMessage order)
        {
            return order.ProductId;
        }

        return string.Empty;
    }
}