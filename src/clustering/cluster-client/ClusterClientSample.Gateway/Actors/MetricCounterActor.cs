using Akka.Actor;
using ClusterClientSample.Shared;

namespace ClusterClientSample.Gateway.Actors;

public class MetricCounterActor : ReceiveActor
{
    public MetricCounterActor()
    {
        var counts = new Dictionary<IActorRef, int>();

        Receive<WorkComplete>(_ =>
        {
            if (counts.TryGetValue(Sender, out var count))
                counts[Sender] = ++count;
            else
                counts.Add(Sender, 1);
        });

        Receive<SendReport>(_ => Sender.Tell(new Report(
            counts.ToDictionary(kvp => kvp.Key.Path.ToString(), kvp => kvp.Value))));
    }
}