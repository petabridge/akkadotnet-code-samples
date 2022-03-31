using System.Data.Common;
using Akka.DependencyInjection;
using Akka.Hosting;
using Akka.SqlInitContainer.Actors;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = new HostBuilder()
    .ConfigureAppConfiguration(c => c.AddEnvironmentVariables())
    .ConfigureServices((context, services) =>
    {
        // maps to environment variable ConnectionStrings__AkkaSqlConnection
        services.AddTransient<DbConnection>(db =>
            new SqlConnection(context.Configuration.GetConnectionString("AkkaSqlConnection")));

        services.AddAkka("SqlConnectionChecker", (configurationBuilder, provider) =>
        {
            configurationBuilder.StartActors((system, registry) =>
            {
                var sqlProps = DependencyResolver.For(system).Props<SqlConnectionChecker>();
                system.ActorOf(sqlProps, "sql-connection-checker");
            });
        });
    })
    .Build();

await builder.RunAsync();