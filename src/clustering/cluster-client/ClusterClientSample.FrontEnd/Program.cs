using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using ClusterClientSample.FrontEnd.Actors;
using Microsoft.Extensions.Hosting;

const string gatewaySystemAddress = "akka.tcp://cluster-system@localhost:12552";
const string systemName = "remote-cluster-system";
const string hostName = "localhost";
const int port = 12553;

Console.Title = "Frontend Gateway Node";

var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAkka(systemName, builder =>
        {
            builder
                // Setup remoting
                .WithRemoting(configure: options =>
                {
                    options.Port = port;
                    options.HostName = hostName;
                })
                
                // Setup `ClusterClient` actor to connect to another actor system
                .WithClusterClient<GatewayClusterClientActor>([ $"{gatewaySystemAddress}/system/receptionist" ])
                
                // Setup required actors and startup code
                .WithActors((system, registry, resolver) =>
                {
                    var requesterActor = system.ActorOf(resolver.Props(typeof(BatchedWorkRequester)), "work-batch-requester");
                    registry.Register<BatchedWorkRequester>(requesterActor);

                    var reportCollectorActor = system.ActorOf(resolver.Props(typeof(WorkReportCollector)), "work-report-collector");
                    registry.Register<WorkReportCollector>(reportCollectorActor);
                });
        });
    }).Build();

await host.StartAsync();
await host.WaitForShutdownAsync();