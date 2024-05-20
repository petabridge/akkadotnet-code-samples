using Akka.Actor;
using Akka.Cluster.Routing;
using Akka.Event;
using Akka.Hosting;
using Akka.Routing;
using ClusterClientSample.Shared;

namespace ClusterClientSample.Gateway.Actors;

public class WorkerManagerActor : ReceiveActor
{
    private static int _nextId = 1;
    
    public WorkerManagerActor(IRequiredActor<MetricCounterActor> counterActor)
    {
        var log = Context.GetLogger();
        var counter = counterActor.ActorRef;
        var workerRouter = GetWorkerRouter(counter);

        Receive<BatchedWork>(batch =>
        {
            log.Info("Generating a work batch of size {0}", batch.Size);
            for (var i = 0; i < batch.Size; i++)
            {
                // forward the work request as if it was sent by the original sender so that the work result can be
                // sent back to the original sender by the worker
                workerRouter.Forward(new Work(_nextId++));
            }
        });
    }

    private static IActorRef GetWorkerRouter(IActorRef counter)
    {
        // Creates a cluster router pool of 10 workers for each cluster node with the role "worker"
        // that joins the cluster.
        //
        // The router will use a round-robin strategy to distribute messages amongst the worker actors
        var props = new ClusterRouterPool(
                local: new RoundRobinPool(10),
                settings: new ClusterRouterPoolSettings(30, 10, true, "worker"))
            .Props(Props.Create(() => new WorkerActor(counter)));
        
        return Context.ActorOf(props);
    }
}