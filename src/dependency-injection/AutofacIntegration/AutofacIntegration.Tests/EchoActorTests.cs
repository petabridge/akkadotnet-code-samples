using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Akka.TestKit;
using Autofac;
using AutoFacIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AutofacIntegration.Tests;

public class EchoActorTests : TestKit
{
    // Created during host startup (ConfigureAkka), once the ActorSystem exists,
    // not in the constructor. The constructor runs before InitializeAsync, so
    // CreateTestProbe() there throws NullReferenceException.
    private TestProbe? _echoProbe;

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // Replace the Autofac-container-registered AutofacInjected with a DI-registered
        // instance. EchoActor is constructed from the IServiceProvider, so this is the
        // instance it receives (the test-side "fake" for the Autofac path).
        services.AddSingleton<AutofacInjected>(new AutofacInjected());

        // EchoActor also requires an Autofac lifetime scope to be resolvable, and its
        // PreStart resolves AutofacInjected from that scope. Register the same type the
        // sample's ConfigureContainer registers so the scope can resolve AutofacInjected.
        var builder = new ContainerBuilder();
        builder.RegisterType<AutofacInjected>();
        var container = builder.Build();
        services.AddSingleton<ILifetimeScope>(container.BeginLifetimeScope());
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        // Probes are created here, inside host startup, NOT in the constructor:
        // the ActorSystem only exists after InitializeAsync runs.
        builder.StartActors((system, registry, resolver) =>
        {
            _echoProbe = CreateTestProbe();
            var echoActor = system.ActorOf(resolver.Props<EchoActor>(), "echo-actor");
            registry.Register<EchoActor>(echoActor);
        });
    }

    [Fact]
    public async Task EchoActor_Replies_With_Same_Message()
    {
        var echo = ActorRegistry.Get<EchoActor>();
        var sender = CreateTestProbe();

        // Send from a probe so we control who receives the echoed reply.
        echo.Tell("ping", sender.Ref);

        var reply = sender.ExpectMsg<string>(TimeSpan.FromSeconds(5));
        Assert.Equal("ping", reply);
    }

    [Fact]
    public async Task EchoActor_Registered_In_ActorRegistry()
    {
        var echo = ActorRegistry.Get<EchoActor>();
        Assert.NotNull(echo);

        // The same actor is registered once; resolving for the actor type succeeds.
        var again = ActorRegistry.Get<EchoActor>();
        Assert.Same(echo, again);
    }
}
