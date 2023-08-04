// See https://aka.ms/new-console-template for more information

using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Persistence.Hosting;
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
using Akka.Persistence.SqlServer.Hosting;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var seedDb = Environment.GetEnvironmentVariable("SEED_DB")?.ToLowerInvariant() is "true" ||
             (args.Length > 0 && args[0] == "seed-db");

var builder = new HostBuilder()
    .ConfigureAppConfiguration(c => c.AddEnvironmentVariables()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{environment}.json"))
    .ConfigureServices((context, services) =>
    {
        // maps to environment variable ConnectionStrings__AkkaSqlConnection
        var connectionString = context.Configuration.GetConnectionString("AkkaSqlConnection");
        if (connectionString is null)
            throw new Exception("AkkaSqlConnection setting is missing");

        var akkaSection = context.Configuration.GetSection("Akka");

        // maps to environment variable Akka__ClusterIp
        var hostName = akkaSection.GetValue<string>("ClusterIp", "localhost");

        // maps to environment variable Akka__ClusterPort
        var port = akkaSection.GetValue<int>("ClusterPort", 0);

        var seeds = akkaSection.GetValue<string[]>("ClusterSeeds", new[] { "akka.tcp://SqlSharding@localhost:7918" })
            .ToArray();

        services.AddAkka("SqlSharding", (configurationBuilder, provider) =>
        {
            #region Custom sharding journal options setup

            var shardingJournalOptions = new SqlServerJournalOptions(
                isDefaultPlugin: false, 
                identifier: "sharding")
            {
                ConnectionString = connectionString,
                TableName = "ShardingEventJournal", 
                MetadataTableName = "ShardingMetadata",
                AutoInitialize = true
            };
            shardingJournalOptions.Adapters.AddWriteEventAdapter<MessageTagger>("tagger", new[] { typeof(object) });

            #endregion

            #region Custom sharding snapshot store options setup

            var shardingSnapshotOptions = new SqlServerSnapshotOptions(
                isDefaultPlugin: false, 
                identifier: "sharding")
            {
                ConnectionString = connectionString,
                TableName = "ShardingSnapshotStore",
                AutoInitialize = true
            };

            #endregion
            
            configurationBuilder
                .WithRemoting(hostName, port)
                .AddAppSerialization()
                .WithClustering(new ClusterOptions
                {
                    Roles = new[] { ProductActorProps.SingletonActorRole }, 
                    SeedNodes = seeds
                })
                .WithSqlServerPersistence(
                    connectionString: connectionString,
                    journalBuilder: builder =>
                    {
                        builder.AddWriteEventAdapter<MessageTagger>("product-tagger", new[] { typeof(IProductEvent) });
                    })
                .WithJournalAndSnapshot(shardingJournalOptions, shardingSnapshotOptions)
                .WithShardRegion<ProductMarker>(
                    typeName: "products",
                    entityPropsFactory: ProductTotalsActor.GetProps, 
                    messageExtractor: new ProductMessageRouter(),
                    shardOptions: new ShardOptions
                    {
                        Role = ProductActorProps.SingletonActorRole, 
                        RememberEntities = true,
                        StateStoreMode = StateStoreMode.DData,
                        RememberEntitiesStore = RememberEntitiesStore.Eventsourced,
                        JournalOptions = shardingJournalOptions,
                        SnapshotOptions = shardingSnapshotOptions, 
                        FailOnInvalidEntityStateTransition = true
                    })
                .WithClusterShardingJournalMigrationAdapter(shardingJournalOptions.PluginId)
                .WithSingleton<ProductIndexActor>(
                    singletonName: "product-proxy",
                    propsFactory: (_, _, resolver) => resolver.Props<ProductIndexActor>(),
                    options: new ClusterSingletonOptions
                    {
                        Role = ProductActorProps.SingletonActorRole
                    })
                .WithSingleton<SoldProductIndexActor>(
                    singletonName: "sold-product-proxy",
                    propsFactory: (_, _, resolver) => resolver.Props<SoldProductIndexActor>(),
                    options: new ClusterSingletonOptions
                    {
                        Role = ProductActorProps.SingletonActorRole
                    })
                .WithSingleton<WarningEventIndexActor>(
                    singletonName: "warning-proxy",
                    propsFactory: (_, _, resolver) => resolver.Props<WarningEventIndexActor>(),
                    options: new ClusterSingletonOptions
                    {
                        Role = ProductActorProps.SingletonActorRole
                    })
                .AddPetabridgeCmd(cmd =>
                {
                    cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                    cmd.RegisterCommandPalette(ClusterCommands.Instance);
                })
                .AddStartup((system, registry) =>
                {
                    if (!seedDb) 
                        return;
                    
                    new FakeDataGenerator().Generate(system, registry, 100);
                });
        });
    })
    .Build();

await builder.RunAsync();