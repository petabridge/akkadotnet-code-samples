using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Event;
using Akka.Hosting;
using ClusterClientSample.Shared;

namespace ClusterClientSample.FrontEnd.Actors;

/// <summary>
/// This actor will:
/// * periodically send a `BatchedWork` request message to the backend app "/user/worker-manager"
/// service via the `ClusterClient` actor
/// * receive the `Result` message for each work performed and logs them
/// </summary>
public class BatchedWorkRequester: ReceiveActor, IWithTimers
{
    private const string BatchKey = nameof(BatchKey);
    
    private readonly Random _random = new();
    
    public BatchedWorkRequester(IRequiredActor<GatewayClusterClientActor> clusterClientActor)
    {
        var log = Context.GetLogger();
        var clusterClient = clusterClientActor.ActorRef;
        
        Receive<BatchedWork>(msg =>
        {
            log.Info("Requesting a batched work to the other cluster. Count: {0}", msg.Size);
            clusterClient.Tell(new ClusterClient.Send("/user/worker-manager", msg, true));
            Timers.StartSingleTimer(BatchKey, GetBatch(), TimeSpan.FromSeconds(10));
        });

        Receive<Result>(msg =>
        {
            log.Info("[ID:{0}] Work result: {1}", msg.Id, msg.Value);
        });
    }

    protected override void PreStart()
    {
        base.PreStart();
        Timers.StartSingleTimer(BatchKey, GetBatch(), TimeSpan.FromSeconds(3));
    }

    public ITimerScheduler Timers { get; set; } = null!;

    private BatchedWork GetBatch() => new (_random.Next(5) + 5);
}