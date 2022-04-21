using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.Streams;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;
using ReliableRabbitMQ.Shared.Messages;
using ReliableRabbitMQ.Shared.Queues;

namespace ReliableRabbitMQ.Consumer.Actors;

public sealed class RabbitMqConsumerActor : ReceiveActor
{
    private readonly AmqpConnectionDetails _connectionDetails;
    private readonly IActorRef _productsShardRegion;

    private readonly ILoggingAdapter _log = Context.GetLogger();

    public RabbitMqConsumerActor(AmqpConnectionDetails connectionDetails, ActorRegistry registry)
    {
        _connectionDetails = connectionDetails;
        _productsShardRegion = registry.Get<ProductActor>();

        Receive<FailedProcess>(f =>
        {
            _log.Info("Failed to process {0}", f);
        });

        Receive<SuccessProcess>(s =>
        {
            _log.Info("Successfully processed and ACKed {0}", s);
        });

        Receive<Complete>(c =>
        {
            _log.Warning("RabbitMQ production stopped. We are probably shutting down.");
        });
    }

    private interface IProcessResult
    {
        string OrderId { get; }
        bool Success { get; }
    }

    private sealed record FailedProcess(string OrderId) : IProcessResult
    {
        public bool Success  => false;
    }

    private sealed record SuccessProcess(string OrderId) : IProcessResult
    {
        public bool Success  => true;
    }

    private sealed class Complete
    {
        public static readonly Complete Instance = new Complete();
        private Complete(){}
    }

    protected override void PreStart()
    {
        var materializer = Context.Materializer();
        var serialization = Context.System.Serialization;
        var serializer = serialization.FindSerializerForType(typeof(CreateOrder));

        var (queueName, queueDeclarations) = QueueSettings.CreateOrdersQueue();

        // stream will run indefinitely and will be automatically killed when this actor stops.
        var source = AmqpSource.CommittableSource(NamedQueueSourceSettings.Create(_connectionDetails, queueName)
                .WithDeclarations(queueDeclarations), 50)
            .SelectAsyncUnordered(50, async c =>
            {
                var msg = serializer.FromBinary<CreateOrder>(c.Message.Bytes.ToArray());
                try
                {
                    var result = await _productsShardRegion.Ask<OrderCommandAck>(msg, TimeSpan.FromSeconds(3));
                    await c.Ack();
                    return (IProcessResult)(new SuccessProcess(msg.OrderId));
                }
                catch (Exception e)
                {
                    _log.Info(e, "Operation [{0}] failed - retrying.", msg.OrderId);
                    await c.Nack();
                    return new FailedProcess(msg.OrderId);
                }
            })
            .RunWith(Sink.ActorRef<IProcessResult>(Self, Complete.Instance), materializer);
    }
}