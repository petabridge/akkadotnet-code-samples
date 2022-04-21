﻿// See https://aka.ms/new-console-template for more information

using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Akka.Streams.Amqp.RabbitMq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReliableRabbitMQ.Consumer.Actors;
using ReliableRabbitMQ.Shared;
using ReliableRabbitMQ.Shared.Clustering;
using ReliableRabbitMQ.Shared.Queues;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

var builder = new HostBuilder()
    .ConfigureAppConfiguration(c => c.AddEnvironmentVariables()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{environment}.json"));
        
builder.ConfigureServices((context, services) =>
{
    services.AddSingleton<AmqpConnectionDetails>(sp =>
    {
        var rabbitMQConfiguration = context.Configuration.GetRequiredSection("RabbitMQ").Get<RabbitMQSettings>();
        var connectionDetails = QueueSettings.CreateConnection(rabbitMQConfiguration);
        return connectionDetails;
    });

    var clusterConfig = context.Configuration.GetRequiredSection("AkkaCluster").Get<AkkaNetworkSettings>();

    services.AddAkka(clusterConfig.ActorSystemName, (configurationBuilder, provider) =>
    {
        configurationBuilder.WithRemoting(clusterConfig.ClusterIp, clusterConfig.ClusterPort)
            .WithClustering(new ClusterOptions()
            {
                Roles = new[] { "shard-host" }, SeedNodes = clusterConfig.ClusterSeeds.Select(Address.Parse).ToArray()
            })
            .WithShardRegion<ProductActor>("products", s => ProductActor.CreateProps(s), new ProductShardMessageExtractor(), 
                new ShardOptions()
                {
                    RememberEntities = false,
                    Role = "shard-host",
                    StateStoreMode = StateStoreMode.DData
                });
    });
});

var host = builder.Build();

await host.RunAsync();