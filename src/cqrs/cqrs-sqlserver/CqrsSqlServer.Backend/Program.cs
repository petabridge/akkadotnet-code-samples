using Akka.Actor;
using Akka.Hosting;
using Akka.Persistence.SqlServer.Hosting;
using CqrsSqlServer.Backend.Actors;
using CqrsSqlServer.DataModel;
using CqrsSqlServer.Shared;
using CqrsSqlServer.Shared.Serialization;
using CqrsSqlServer.Shared.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
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
                .WithSqlServerPersistence(connectionString)
                .WithActors((system, registry, resolver) =>
                {
                    var parentActor =
                        system.ActorOf(
                            Props.Create(() => new GenericChildPerEntityParent(new ProductMessageRouter(),
                                ProductTotalsActor.GetProps)), "productTotals");
                    
                    registry.Register<ProductMarker>(parentActor);
                })
                .AddPetabridgeCmd(cmd =>
                {

                })
                .AddStartup((system, registry) =>
                {
                    new FakeDataGenerator().Generate(system, registry, 100);
                });;
        });
    });

await hostBuilder.Build().RunAsync();