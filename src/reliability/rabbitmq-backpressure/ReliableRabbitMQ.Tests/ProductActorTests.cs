using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Akka.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReliableRabbitMQ.Consumer.Actors;
using ReliableRabbitMQ.Shared.Messages;
using Xunit;

namespace ReliableRabbitMQ.Tests;

/// <summary>
/// Unit tests for <see cref="ProductActor"/>.
///
/// ProductActor is the only actor in the sample that can be unit-tested without a
/// live broker. The RabbitMqConsumerActor and AmqpProducerActor both open live AMQP
/// streams inside <c>PreStart</c> and are NOT unit-testable without a RabbitMQ broker;
/// they are intentionally not covered here.
///
/// ProductActor has a nondeterministic behaviour: <see cref="ProductActor.SimulateNetworkFailure"/>
/// randomly drops about half of incoming <see cref="CreateOrder"/> messages without replying.
/// To keep the test deterministic we drive the success path only, retrying with fresh
/// distinct orders until a reply is observed. The probability that a recognizable number
/// of consecutive attempts all fail is negligible (0.5^n), so the test is effectively
/// deterministic while remaining robust to the actor's random dropout.
/// </summary>
public class ProductActorTests : TestKit
{
    private const string ProductId = "prod-1";

    // Created during host startup (ConfigureAkka), once the ActorSystem exists, not in the
    // constructor. The constructor runs before InitializeAsync, so CreateTestProbe() there
    // throws NullReferenceException.
    private TestProbe? _senderProbe;

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry, resolver) =>
        {
            _senderProbe = CreateTestProbe();
            var productActor = system.ActorOf(ProductActor.CreateProps(ProductId), "product");
            registry.Register<ProductActor>(productActor);
        });
    }

    [Fact]
    public async Task ProductActor_Acknowledges_Order_When_Network_Is_Healthy()
    {
        var actor = ActorRegistry.Get<ProductActor>();

        // Drive the deterministic success path: keep issuing fresh distinct orders
        // (ProductActor drops ~50% of them randomly without replying) until we observe
        // an OrderCommandAck. Failure probability across the attempts is negligible.
        OrderCommandAck? observed = null;
        for (var i = 0; i < 200 && observed is null; i++)
        {
            var order = new CreateOrder(Guid.NewGuid().ToString(), "customer", ProductId, 2);

            // Tell from the probe so we control who receives the reply, just like the
            // RabbitMqConsumerActor does via ShardRegion Tell.
            actor.Tell(order, _senderProbe!.Ref);

            // If the order was not randomly dropped, ProductActor replies with an ack
            // addressed to the sender. Wait a short window; a null means this order was
            // dropped by the simulated network failure, so try the next one.
            if (_senderProbe.ReceiveOne(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken) is OrderCommandAck ack)
                observed = ack;
        }

        Assert.NotNull(observed);
        Assert.Equal(ProductId, observed!.ProductId);
    }
}
