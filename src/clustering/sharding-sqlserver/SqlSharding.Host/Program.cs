// See https://aka.ms/new-console-template for more information

using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.SqlServer.Hosting;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using SqlSharding.Host.Actors;
using SqlSharding.Shared;
using SqlSharding.Shared.Events;
using SqlSharding.Shared.Serialization;
using SqlSharding.Shared.Sharding;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

var builder = new HostBuilder()
    .ConfigureAppConfiguration(c => c.AddEnvironmentVariables()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{environment}.json"))
    .ConfigureServices((context, services) =>
    {
        // maps to environment variable ConnectionStrings__AkkaSqlConnection
        var connectionString = context.Configuration.GetConnectionString("AkkaSqlConnection");


        var akkaSection = context.Configuration.GetSection("Akka");

        // maps to environment variable Akka__ClusterIp
        var hostName = akkaSection.GetValue<string>("ClusterIp", "localhost");

        // maps to environment variable Akka__ClusterPort
        var port = akkaSection.GetValue<int>("ClusterPort", 0);

        var seeds = akkaSection.GetValue<string[]>("ClusterSeeds", new[] { "akka.tcp://SqlSharding@localhost:7918" })

            .ToArray();

        services.AddAkka("SqlSharding", (configurationBuilder, provider) =>
        {
            configurationBuilder
                .WithRemoting(hostName, port)
                .AddAppSerialization()
                .WithClustering(new ClusterOptions()
                    { Roles = new[] { ProductActorProps.SingletonActorRole }, SeedNodes = seeds })
                .WithSqlServerPersistence(connectionString, journalBuilder: builder =>
                {
                    builder.AddWriteEventAdapter<MessageTagger>("product-tagger", new[] { typeof(IProductEvent) });
                })
                .WithShardRegion<ProductMarker>("products",
                    s => ProductTotalsActor.GetProps(s), new ProductMessageRouter(),
                    new ShardOptions()
                    {
                        Role = ProductActorProps.SingletonActorRole, RememberEntities = true,
                        StateStoreMode = StateStoreMode.DData,
                        RememberEntitiesStore = RememberEntitiesStore.Eventsourced,
                        JournalPluginId = "akka.persistence.journal.sharding",
                        SnapshotPluginId = "akka.persistence.snapshot-store.sharding"
                    })
                .WithClusterShardingJournalMigrationAdapter("akka.persistence.journal.sharding")
                .AddHoconFile("sharding.conf", HoconAddMode.Prepend)
                .AddHocon(@$"akka.persistence.journal.sharding.connection-string = ""{connectionString}""
                akka.persistence.snapshot-store.sharding.connection-string = ""{connectionString}""
                ", HoconAddMode.Prepend)
                .WithSingleton<ProductIndexActor>("product-proxy",
                    (_, _, resolver) => resolver.Props<ProductIndexActor>(),
                    new ClusterSingletonOptions() { Role = ProductActorProps.SingletonActorRole })
                // .WithSingleton<ProductCreatorActor>("product-creator",
                //     (system, registry, resolver) => resolver.Props<ProductCreatorActor>(21_000),
                //     new ClusterSingletonOptions() { Role = ProductActorProps.SingletonActorRole })
                .AddPetabridgeCmd(cmd =>
                {
                    cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                    cmd.RegisterCommandPalette(ClusterCommands.Instance);
                });
        });
    })
    .Build();

await builder.RunAsync();