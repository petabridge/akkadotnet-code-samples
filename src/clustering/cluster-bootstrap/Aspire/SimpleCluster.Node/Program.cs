using Akka.Cluster.Hosting;
using Akka.Discovery.Azure;
using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SimpleCluster.Node;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // bind environment configuration passed in from Aspire
        var akkaOptions = new AkkaOptions();
        context.Configuration.GetSection("Akka").Bind(akkaOptions);
        
        services.AddAkka("SimpleCluster", builder =>
        {
            // Setup Akka.Remote
            builder.WithRemoting(opt =>
            {
                opt.PublicHostName = akkaOptions.HostName;
                opt.PublicPort = akkaOptions.RemotePort;
                opt.HostName = "0.0.0.0";
                opt.Port = akkaOptions.RemotePort;
            });
            
            // Setup Akka.Cluster
            builder.WithClustering();
            
            // Setup Akka.Management
            builder.WithAkkaManagement(opt =>
            {
                opt.Http.HostName = akkaOptions.HostName;
                opt.Http.Port = akkaOptions.ManagementPort;
                opt.Http.BindHostName = "0.0.0.0";
                opt.Http.BindPort = akkaOptions.ManagementPort;
            });
            
            // Setup Akka.Management.Cluster.Bootstrap
            builder.WithClusterBootstrap(opt =>
            {
                opt.ContactPointDiscovery.ServiceName = akkaOptions.DiscoveryServiceName;
                opt.ContactPointDiscovery.RequiredContactPointsNr = 3;
                
                // NOTE: It is important to set this to false
                // see https://getakka.net/articles/discovery/akka-management.html#running-clustered-nodes-on-a-single-machine
                opt.ContactPoint.FilterOnFallbackPort = false;
            });
            
            // Setup Akka.Discovery.Azure
            var connectionString = context.Configuration.GetConnectionString("azure-tables") ?? "UseDevelopmentStorage=true";
            builder.WithAzureDiscovery(opt =>
            {
                opt.ConnectionString = connectionString;
                opt.HostName = akkaOptions.HostName;
                opt.Port = akkaOptions.ManagementPort;
                opt.ServiceName = akkaOptions.DiscoveryServiceName;
            });
        });
    })
    .UseConsoleLifetime()
    .RunConsoleAsync();