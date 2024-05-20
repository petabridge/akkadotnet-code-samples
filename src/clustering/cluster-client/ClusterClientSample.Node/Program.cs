using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Hosting;

const string systemName = "cluster-system";
const string hostName = "localhost";
const int port = 0;
const string gateway = $"akka.tcp://{systemName}@{hostName}:12552";

Console.Title = "Backend Worker Node";

// Note that we did not start any actors in the nodes, all actors will be deployed using remoting
var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddAkka(systemName, builder =>
        {
            builder
                // Setup remoting and clustering
                .WithRemoting(configure: options =>
                {
                    options.Port = port;
                    options.HostName = hostName;
                })
                .WithClustering(options: new ClusterOptions
                {
                    // Giving this cluster node the role/tag "worker" signals that the gateway node can
                    // deploy worker actors in this node using remoting
                    Roles = ["worker"],
                    SeedNodes = [gateway]
                });
        });
    }).Build();

await host.StartAsync();
await host.WaitForShutdownAsync();