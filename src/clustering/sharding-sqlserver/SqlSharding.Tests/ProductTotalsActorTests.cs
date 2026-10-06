using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Akka.Persistence.Hosting;
using Akka.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlSharding.Host.Actors;
using SqlSharding.Shared;
using SqlSharding.Shared.Commands;
using SqlSharding.Shared.Queries;
using Xunit;

namespace SqlSharding.Tests;

/// <summary>
/// Exercises <see cref="ProductTotalsActor"/>, the persistent (ReceivePersistentActor)
/// shard entity, against the in-memory journal/snapshot store. These tests confirm
/// command handling, the CommandResponse contract, and that state is recovered by
/// replaying persisted events after an actor restart — WITHOUT requiring SQL Server.
/// </summary>
public class ProductTotalsActorTests : TestKit
{
    // Entity id used as the actor's persistenceId argument. ProductTotalsActor
    // prepends the "totals-" entity-name constant to form the real PersistenceId.
    private const string EntityId = "product-foo";

    // Created inside ConfigureAkka's WithActors callback (NOT the constructor).
    // The TestKit base initializes Sys during InitializeAsync, which runs after
    // the constructor, so CreateTestProbe() in the constructor throws NRE.
    private TestProbe? _sender;
    private IActorRef? _totalsActor;

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder
            .WithInMemoryJournal()
            .WithInMemorySnapshotStore()
            .WithActors((system, registry, resolver) =>
            {
                _sender = CreateTestProbe();
                _totalsActor = system.ActorOf(resolver.Props<ProductTotalsActor>(EntityId), "totals-actor");
            });
    }

    [Fact]
    public async Task CreateProduct_Persists_And_Replies_With_CommandResponse()
    {
        var actor = _totalsActor!;
        actor.Tell(new CreateProduct(EntityId, "Foo", 10.99m, 5, new[] { "tag-a" }), _sender!.Ref);

        var response = _sender.ExpectMsg<ProductCommandResponse>(TimeSpan.FromSeconds(5));
        Assert.True(response.Success);
        Assert.Equal(EntityId, response.ProductId);
        // CreateProduct yields ProductCreated + ProductInventoryChanged.
        Assert.Equal(2, response.ResponseEvents.Count);
    }

    [Fact]
    public async Task SupplyProduct_Updates_Inventory_Totals()
    {
        var actor = _totalsActor!;
        actor.Tell(new CreateProduct(EntityId, "Foo", 10.99m, 5, new[] { "tag-a" }), _sender!.Ref);
        _sender.ExpectMsg<ProductCommandResponse>(TimeSpan.FromSeconds(5));

        actor.Tell(new SupplyProduct(EntityId, 10), _sender.Ref);
        var supply = _sender.ExpectMsg<ProductCommandResponse>(TimeSpan.FromSeconds(5));
        Assert.True(supply.Success);

        // Verify the projected totals via the query.
        actor.Tell(new FetchProduct(EntityId), _sender.Ref);
        var result = _sender.ExpectMsg<FetchResult>(TimeSpan.FromSeconds(5));
        Assert.Equal(15, result.State.Totals.RemainingInventory);
        Assert.Equal("Foo", result.State.Data.ProductName);
    }

    [Fact]
    public async Task Restart_Replays_Events_And_Recovers_State()
    {
        var actor = _totalsActor!;
        // Build up some persisted state.
        actor.Tell(new CreateProduct(EntityId, "Foo", 10.99m, 5, new[] { "tag-a" }), _sender!.Ref);
        _sender.ExpectMsg<ProductCommandResponse>(TimeSpan.FromSeconds(5));

        actor.Tell(new SupplyProduct(EntityId, 10), _sender.Ref);
        var supply = _sender.ExpectMsg<ProductCommandResponse>(TimeSpan.FromSeconds(5));
        Assert.True(supply.Success);

        // Stop the actor and wait for termination before recreating it with the
        // same persistenceId so recovery must come from the journal replay.
        Watch(actor);
        actor.Tell(PoisonPill.Instance);
        ExpectTerminated(actor, TimeSpan.FromSeconds(5));

        var recovered = Sys.ActorOf(Props.Create(() => new ProductTotalsActor(EntityId)), "totals-actor-recovered");

        // After restart, querying must return the replayed state (remaining = 15).
        recovered.Tell(new FetchProduct(EntityId), _sender.Ref);
        var result = _sender.ExpectMsg<FetchResult>(TimeSpan.FromSeconds(5));
        Assert.Equal(15, result.State.Totals.RemainingInventory);
        Assert.Equal("Foo", result.State.Data.ProductName);
    }
}
