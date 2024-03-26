using Akka.Actor;
using Akka.Hosting;
using Akka.Persistence.Sql.Config;
using CqrsSqlServer.Backend.Actors;
using CqrsSqlServer.DataModel;
using CqrsSqlServer.Shared;
using CqrsSqlServer.Shared.Events;
using CqrsSqlServer.Shared.Serialization;
using CqrsSqlServer.Shared.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Akka.Persistence.Sql.Hosting;
using LinqToDB;
using Petabridge.Cmd.Host;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

var hostBuilder = new HostBuilder()
    .ConfigureAppConfiguration((hostContext, configApp) =>
    {
        configApp.AddEnvironmentVariables()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json");
    })
    .ConfigureServices((hostContext, services) =>
    {
        var connectionString = hostContext.Configuration.GetConnectionString("AkkaSqlConnection");
        if (connectionString is null)
            throw new Exception("AkkaSqlConnection setting is missing");

        services.AddDbContext<CqrsSqlServerContext>(options =>
        {
            // disable change tracking for all implementations of this context
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.UseSqlServer(connectionString);
        });

        services.AddAkka("CqrsProjections", (builder, provider) =>
        {
            builder
                .AddAppSerialization()
                .WithSqlPersistence(connectionString: connectionString,
                    providerName: ProviderName.SqlServer2019,
                    databaseMapping: DatabaseMapping.SqlServer,
                    tagStorageMode: TagMode.TagTable,
                    journalBuilder: journalBuilder =>
                    {
                        journalBuilder.AddWriteEventAdapter<MessageTagger>("product-tagger",
                            new[] { typeof(IProductEvent) });
                    })
                .WithActors((system, registry, resolver) =>
                {
                    // in a real world scenario, this actor would be an Akka.Cluster.Sharding ShardRegion
                    var parentActor =
                        system.ActorOf(
                            Props.Create(() => new GenericChildPerEntityParent(new ProductMessageRouter(),
                                ProductTotalsActor.GetProps)), "productTotals");

                    registry.Register<ProductMarker>(parentActor);
                })
                .WithActors((system, registry, resolver) =>
                {
                    // in a real-world scenario, this projector would be a cluster singleton or would run in its own process
                    var projectionsActor = system.ActorOf(resolver.Props<ProductProjectorActor>(), "projections");
                })
                .AddPetabridgeCmd(cmd => { })
                .AddStartup((system, registry) =>
                {
                    var seedDb = hostContext.Configuration.GetValue<bool>("SeedDb");
                    if (seedDb)
                        new FakeDataGenerator().Generate(system, registry, 100);
                });
            ;
        });
    });

await hostBuilder.Build().RunAsync();