using Akka.Util;

namespace ReliableRabbitMQ.Shared.Messages;

public interface IOrderMessage
{
    public string OrderId { get; }
}

public interface IProductMessage
{
    public string ProductId { get; }
}

public sealed record CreateOrder(string OrderId, string CustomerId, string ProductId, int Quantity) : IOrderMessage, IProductMessage
{
    public static CreateOrder Random()
    {
        return new CreateOrder(Guid.NewGuid().ToString(), ThreadLocalRandom.Current.Next(0, 10000).ToString(),
            ThreadLocalRandom.Current.Next(0, 100).ToString(), ThreadLocalRandom.Current.Next(1, 10));
    }
}

public sealed record OrderCommandAck(string OrderId, string ProductId) : IOrderMessage, IProductMessage;