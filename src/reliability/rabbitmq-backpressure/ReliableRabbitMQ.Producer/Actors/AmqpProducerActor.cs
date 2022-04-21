using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;
using ReliableRabbitMQ.Shared.Messages;
using ReliableRabbitMQ.Shared.Queues;

namespace ReliableRabbitMQ.Producer.Actors;

/// <summary>
/// Actor responsible for managing the AMQP production stream
/// </summary>
public sealed class AmqpProducerActor : ReceiveActor, IWithTimers
{
    private readonly AmqpConnectionDetails _connectionDetails;

    // can be used to cancel the Source.Tick
    private ICancelable _cancelable;
    private long _totalMessages = 0L;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public AmqpProducerActor(AmqpConnectionDetails connectionDetails)
    {
        _connectionDetails = connectionDetails;

        Receive<int>(i =>
        {
            _totalMessages += i;
        });

        Receive<CheckStats>(_ =>
        {
            _log.Info("Wrote [{0}] messages to Rabbit queue [{1}]", _totalMessages, QueueSettings.InputQueueName);
            _totalMessages = 0;
        });
    }

    protected override void PreStart()
    {
        var materializer = Context.Materializer();
        var serialization = Context.System.Serialization;
        var (cancel, source) = Source.Tick(TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(100), NotUsed.Instance)
            .Select(c => CreateOrder.Random())
            .Select(o => ByteString.FromBytes(serialization.Serialize(o)))
            .PreMaterialize(materializer);

        _cancelable = cancel;

        var source2 = source
            .AlsoTo<ByteString, NotUsed>(Flow.Create<ByteString>().Select(b => 1).To(Sink.ActorRef<int>(Self, "complete")))
            .Select(o => new OutgoingMessage(o, true, true));


        var (routingKey, queueDeclaration) = QueueSettings.CreateOrdersQueue();

        var sink = AmqpSink.Create(AmqpSinkSettings.Create(_connectionDetails)
            .WithRoutingKey(routingKey)
            .WithDeclarations(queueDeclaration));

        var task = source2.RunWith(sink, materializer).PipeTo(Self);
        
        Timers.StartPeriodicTimer("check-stats", CheckStats.Instance, TimeSpan.FromSeconds(5));
    }

    private sealed class CheckStats : INoSerializationVerificationNeeded
    {
        public static readonly CheckStats Instance = new CheckStats();
        private CheckStats(){}
    }

    public ITimerScheduler Timers { get; set; }
}