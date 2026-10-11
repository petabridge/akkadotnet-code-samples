using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Akka.TestKit;
using ClusterClientSample.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ClusterClientSample.Tests;

public class WorkerActorTests : TestKit
{
    // Created during host startup (ConfigureAkka), once the ActorSystem exists,
    // not in the constructor. The constructor runs before InitializeAsync, so
    // CreateTestProbe() there throws NullReferenceException.
    private TestProbe? _counterProbe;

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry, resolver) =>
        {
            _counterProbe = CreateTestProbe();
            var worker = system.ActorOf(resolver.Props<WorkerActor>(_counterProbe.Ref), "worker");
            registry.Register<WorkerActor>(worker);
        });
    }

    [Fact]
    public async Task Worker_Processes_Work_And_Replies_With_Result()
    {
        var worker = ActorRegistry.Get<WorkerActor>();
        var sender = CreateTestProbe();

        // Send Work from a probe so we control who receives the reply.
        worker.Tell(new Work(42), sender.Ref);

        // WorkerActor computes Id * 10, replies to the sender.
        var result = sender.ExpectMsg<Result>(TimeSpan.FromSeconds(5));
        Assert.Equal(42, result.Id);
        Assert.Equal(420, result.Value);

        // It also notifies the counter actor.
        _counterProbe!.ExpectMsg<WorkComplete>(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Worker_Notifies_Counter_For_Each_Work()
    {
        var worker = ActorRegistry.Get<WorkerActor>();
        var sender = CreateTestProbe();

        worker.Tell(new Work(1), sender.Ref);
        sender.ExpectMsg<Result>(TimeSpan.FromSeconds(5));
        _counterProbe!.ExpectMsg<WorkComplete>(TimeSpan.FromSeconds(5));

        worker.Tell(new Work(2), sender.Ref);
        sender.ExpectMsg<Result>(TimeSpan.FromSeconds(5));
        _counterProbe!.ExpectMsg<WorkComplete>(TimeSpan.FromSeconds(5));
    }
}
