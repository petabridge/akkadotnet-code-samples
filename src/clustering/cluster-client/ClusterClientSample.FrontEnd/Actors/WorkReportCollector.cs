using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Event;
using Akka.Hosting;
using ClusterClientSample.Shared;

namespace ClusterClientSample.FrontEnd.Actors;

/// <summary>
/// This actor will:
/// * periodically publish a `SendReport` message to the backend app "report" pub-sub topic via the `ClusterClient` actor
/// * receive the `Report` message from the workload metric actor and logs them
/// </summary>
public class WorkReportCollector: ReceiveActor, IWithTimers
{
    private class GetReport
    {
        public static readonly GetReport Instance = new();
        private GetReport() { }
    }
    
    private const string ReportKey = nameof(ReportKey);
    
    public WorkReportCollector(IRequiredActor<GatewayClusterClientActor> clusterClientActor)
    {
        var log = Context.GetLogger();
        var clusterClient = clusterClientActor.ActorRef;
        
        Receive<Report>(report =>
        {
            foreach (var (actor, count) in report.Counts)
            {
                log.Info("Worker {0} has done {1} works", actor, count);
            }
        });
        
        Receive<GetReport>(_ =>
        {
            log.Info("Requesting work report metrics from the other cluster.");
            clusterClient.Tell(new ClusterClient.Publish("report", SendReport.Instance));
        });
    }

    public ITimerScheduler Timers { get; set; } = null!;

    protected override void PreStart()
    {
        base.PreStart();
        Timers.StartPeriodicTimer(ReportKey, GetReport.Instance, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
    }
}