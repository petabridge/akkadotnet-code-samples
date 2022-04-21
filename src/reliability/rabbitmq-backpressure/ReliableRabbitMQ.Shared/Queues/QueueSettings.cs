using Akka.Streams.Amqp.RabbitMq;

namespace ReliableRabbitMQ.Shared.Queues;

/// <summary>
/// Shared settings for queue data
/// </summary>
public static class QueueSettings
{
    public const string InputQueueName = "orders-queue";

    public static AmqpConnectionDetails CreateConnection(RabbitMQSettings settings)
    {
        return AmqpConnectionDetails.Create(settings.Host, settings.Port)
            .WithCredentials(AmqpCredentials.Create(settings.Username, settings.Password))
            .WithAutomaticRecoveryEnabled(true)
            .WithNetworkRecoveryInterval(TimeSpan.FromSeconds(1));
    }

    public static (string routingKey, QueueDeclaration declaration) CreateOrdersQueue()
    {
        return (InputQueueName, QueueDeclaration.Create(InputQueueName).WithDurable(true).WithAutoDelete(false));
    }
}