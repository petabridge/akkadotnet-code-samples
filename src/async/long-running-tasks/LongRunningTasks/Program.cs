using Akka.Actor;
using Akka.Hosting;
using LongRunningTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("ImportSystem", (builder, sp) =>
        {
            builder.WithActors((system, registry, resolver) =>
            {
                var importActor = system.ActorOf(
                    resolver.Props<BatchImportActor>(), "batch-importer");
                registry.Register<BatchImportActor>(importActor);
            });
        });
    })
    .Build();

await host.StartAsync();

var actorRegistry = host.Services.GetRequiredService<ActorRegistry>();
var importActor = actorRegistry.Get<BatchImportActor>();

Console.WriteLine("=== Akka.NET Long-Running Tasks Demo ===");
Console.WriteLine("Commands:");
Console.WriteLine("  start [count]  - Generate fake data and start import (default: 10000)");
Console.WriteLine("  status         - Query actor status while import runs");
Console.WriteLine("  cancel         - Cancel the running import");
Console.WriteLine("  quit           - Graceful shutdown");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(input)) continue;

    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    switch (parts[0].ToLowerInvariant())
    {
        case "start":
            var count = parts.Length > 1 && int.TryParse(parts[1], out var c) ? c : 10_000;
            importActor.Tell(new StartImport(count));
            Console.WriteLine($"  Sent StartImport({count:N0}) to actor.");
            break;

        case "status":
            try
            {
                var status = await importActor.Ask<StatusResponse>(
                    new GetStatus(), TimeSpan.FromSeconds(3));
                Console.WriteLine($"  Active:   {status.IsActive}");
                Console.WriteLine($"  Progress: {status.RecordsProcessed:N0} / {status.TotalRecords:N0}");
                if (status.TotalRecords > 0)
                {
                    var pct = (double)status.RecordsProcessed / status.TotalRecords * 100;
                    Console.WriteLine($"  Complete: {pct:F1}%");
                }
                Console.WriteLine($"  Elapsed:  {status.Elapsed:mm\\:ss\\.ff}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Failed to get status: {ex.Message}");
            }
            break;

        case "cancel":
            importActor.Tell(new CancelImport());
            Console.WriteLine("  Sent CancelImport to actor.");
            break;

        case "quit":
        case "exit":
            Console.WriteLine("  Shutting down...");
            await host.StopAsync();
            return;

        default:
            Console.WriteLine("  Unknown command. Try: start, status, cancel, quit");
            break;
    }
}
