using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Tools.Client;
using Akka.Hosting;
using Akka.Remote.Hosting;
using ClusterClientSample.Gateway.Actors;
using Microsoft.Extensions.Hosting;

const string systemName = "cluster-system";
const string hostName = "localhost";
const string port = "12552";
const string selfAddress = $"akka.tcp://{systemName}@{hostName}:{port}";

// This node acts as a known contact point for all external actor systems to connect via `ClusterClient` by setting up
// the `ClusterClientReceptionist`
Console.Title = "Backend Gateway Node";

var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAkka(systemName, builder =>
        {
            builder
                // Setup remoting and clustering
                .WithRemoting(configure: options =>
                {
                    options.Port = int.Parse(port);
                    options.HostName = hostName;
                })
                .WithClustering(options: new ClusterOptions
                {
                    // Note that we do not assign the role/tag "worker" to this node, no worker actors will be
                    // deployed in this node.
                    //
                    // If we want the gateway to also work double duty as worker node, we can add the "worker" role/tag
                    // to the Roles array.
                    Roles = [ "gateway" ],
                    SeedNodes = [ selfAddress ]
                })
                
                // Setup `ClusterClientReceptionist` to only deploy on nodes with "gateway" role
                .WithClusterClientReceptionist(role: "gateway")
                
                // Setup required actors and startup code
                .WithActors((system, registry, resolver) =>
                {
                    // The name of this actor ("worker-manager") is required, because its absolute path
                    // ("/user/worker-manager") will be used as a service path by ClusterClientReceptionist.
                    //
                    // This name has to be unique for all actor names living in this actor system.
                    var workerManagerActor = system.ActorOf(resolver.Props(typeof(WorkerManagerActor)), "worker-manager");
                    registry.Register<WorkerManagerActor>(workerManagerActor);

                    // The name of this actor ("metric-workload-counter") is optional as it leverages the
                    // distributed pub-sub system. External actor systems does not need to know the actor path to
                    // query the workload metric.
                    var workLoadCounterActor = system.ActorOf(Props.Create(() => new MetricCounterActor()), "metric-workload-counter");
                    registry.Register<MetricCounterActor>(workLoadCounterActor);
                })
                .AddStartup((system, registry) =>
                {
                    var receptionist = ClusterClientReceptionist.Get(system);
                    
                    // Register the worker manager actor as a service,
                    // this can be accessed through "user/worker-manager"
                    var workerManagerActor = registry.Get<WorkerManagerActor>();
                    receptionist.RegisterService(workerManagerActor);

                    // Register the workload counter metric actor as a topic listener, it will subscribe to the
                    // "report" topic
                    var workloadCounterActor = registry.Get<MetricCounterActor>();
                    receptionist.RegisterSubscriber("report", workloadCounterActor);
                });
        });
    }).Build();

await host.StartAsync();
await host.WaitForShutdownAsync();