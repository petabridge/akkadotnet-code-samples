// See https://aka.ms/new-console-template for more information

using Akka;
using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Dispatch.SysMsg;
using Akka.Hosting;
using Akka.Streams;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReliableRabbitMQ.Producer.Actors;
using ReliableRabbitMQ.Shared;
using ReliableRabbitMQ.Shared.Messages;
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
   services.AddAkka("RabbitMQProducer", (configurationBuilder, srv) =>
   {
      configurationBuilder.StartActors((system, registry) =>
      {
         var sp = DependencyResolver.For(system);
         var props = sp.Props<AmqpProducerActor>();
         var producerRef = system.ActorOf(props, "amqp-producer");
         registry.TryRegister<AmqpProducerActor>(producerRef);
      });
   });
});

await builder.Build().RunAsync();