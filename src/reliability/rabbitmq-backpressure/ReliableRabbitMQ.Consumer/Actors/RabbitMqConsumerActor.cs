using Akka.Actor;
using Akka.Event;
using Akka.Streams.Amqp.RabbitMq;

namespace ReliableRabbitMQ.Consumer.Actors;

public sealed class RabbitMqConsumerActor : ReceiveActor
{
    private readonly AmqpConnectionDetails _connectionDetails;

    // can be used to cancel the Source.Tick
    private ICancelable _cancelable;
    private long _totalBytes = 0L;
    private readonly ILoggingAdapter _log = Context.GetLogger();
}