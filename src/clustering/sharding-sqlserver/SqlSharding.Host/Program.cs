// See https://aka.ms/new-console-template for more information

using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Persistence.SqlServer.Hosting;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SqlSharding.Host.Actors;
using SqlSharding.Shared.Sharding;

var builder = new HostBuilder()
    .ConfigureAppConfiguration(c => c.AddEnvironmentVariables())
    .ConfigureServices((context, services) =>
    {
        // maps to environment variable ConnectionStrings__AkkaSqlConnection
        var connectionString = context.Configuration.GetConnectionString("AkkaSqlConnection");


        var akkaSection = context.Configuration.GetSection("Akka");

        // maps to environment variable Akka__ClusterIp
        var hostName = akkaSection.GetValue<string>("ClusterIp", "localhost");

        // maps to environment variable Akka__ClusterPort
        var port = akkaSection.GetValue<int>("ClusterPort", 7919);

        var seeds = akkaSection.GetValue<string[]>("ClusterSeeds", new []{ "akka.tcp://SqlSharding@localhost:7919" }).Select(Address.Parse)
            .ToArray();

        services.AddAkka("SqlSharding", (configurationBuilder, provider) =>
        {
            configurationBuilder
                .WithRemoting(hostName, port)
                .WithClustering(new ClusterOptions()
                    { Roles = new[] { ProductActorProps.SingletonActorRole }, SeedNodes = seeds })
                .WithSqlServerPersistence(connectionString)
                .StartActors((system, registry) =>
                {
                    var indexProps = Props.Create(() => new ProductIndexActor());
                    var singletonProps = system.ProductSingletonProps(indexProps);
                    registry.TryRegister<ProductIndexActor>(system.ActorOf(singletonProps,
                        ProductActorProps.SingletonActorName));

                    // don't really need the ClusterSingletonProxy in this service, but it doesn't hurt to have it
                    // in case we do want to message the Singleton directly from the Host node
                    var proxyProps = system.ProductIndexProxyProps();
                    registry.TryRegister<ProductIndexMarker>(system.ActorOf(proxyProps, "product-proxy"));
                })
                .WithShardRegion<ProductMarker>("products", s => ProductTotalsActor.GetProps(s), new ProductMessageRouter(), 
                    new ShardOptions(){ RememberEntities = true, Role = ProductActorProps.SingletonActorRole, StateStoreMode = StateStoreMode.DData});
        });
    })
    .Build();

await builder.RunAsync();