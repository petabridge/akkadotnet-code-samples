using Akka.Util;

namespace ReliableRabbitMQ.Shared.Messages;

public sealed record CreateOrder(string OrderId, string CustomerId, string ProductId, int Quantity)
{
    public static CreateOrder Random()
    {
        return new CreateOrder(Guid.NewGuid().ToString(), ThreadLocalRandom.Current.Next(0, 10000).ToString(),
            ThreadLocalRandom.Current.Next(0, 100).ToString(), ThreadLocalRandom.Current.Next(1, 10));
    }
}