// -----------------------------------------------------------------------
//  <copyright file="ProductProjectorActor.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Streams;
using Akka.Streams.Dsl;
using CqrsSqlServer.DataModel;
using CqrsSqlServer.DataModel.Entities;
using CqrsSqlServer.Shared;
using CqrsSqlServer.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using static CqrsSqlServer.Backend.Actors.ExponentialBackoffTimeout;

namespace CqrsSqlServer.Backend.Actors;

public sealed record ProjectionFailed(Exception Cause);

public sealed class ProjectionCompleted
{
    public static readonly ProjectionCompleted Instance = new();
}

public sealed class ProjectionAck
{
    public static readonly ProjectionAck Instance = new();
}

public sealed class ProjectionStarting
{
    public static readonly ProjectionStarting Instance = new();
}

public record MaterializedViewState(Offset LastOffset);

public sealed class ProductProjectorActor : ReceivePersistentActor
{
    public override string PersistenceId { get; }

    public MaterializedViewState CurrentState { get; set; }
    private const int MaxRetryAttempts = 3;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IServiceProvider _serviceProvider;

    public ProductProjectorActor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        PersistenceId = "product-projector";

        Recovers();
    }

    private void Recovers()
    {
        Recover<MaterializedViewState>(s => { CurrentState = s; });

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is MaterializedViewState state)
            {
                CurrentState = state;
            }
        });
    }

    protected override void OnReplaySuccess()
    {
        var readJournal = PersistenceQuery.Get(Context.System)
            .ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
        var self = Self;
        var sink = Sink.ActorRefWithAck<EventEnvelope>(self, ProjectionStarting.Instance, ProjectionAck.Instance,
            ProjectionCompleted.Instance, ex => new ProjectionFailed(ex));

        readJournal.EventsByTag(MessageTagger.ProductEventTag, CurrentState.LastOffset)
            .RunWith(sink, Context.Materializer());
    }

    private void Commands()
    {
        CommandAsync<EventEnvelope>(async e =>
        {
            _log.Info("Received envelope with offset [{0}]", e.Offset);
            var currentOffset = e.Offset;

            if (e.Event is IProductEvent publisherEvent)
            {
                var initialTime = TimeSpan.FromSeconds(3);
                var attempts = 0;
                while (attempts < MaxRetryAttempts)
                {
                    var timeout = BackoffTimeout(attempts, initialTime);
                    var ex = await TryProcess(publisherEvent, timeout);
                    if (ex.HasValue)
                    {
                        _log.Warning(ex.Value,
                            "Failed to project event [{0}] on attempt #[{1}] after [{2}] seconds. Retrying in [{3}] with [{4}] attempts remaining.",
                            publisherEvent, attempts, timeout, BackoffTimeout(attempts + 1, initialTime),
                            MaxRetryAttempts - attempts);
                        attempts++;
                    }
                    else
                    {
                        // success
                        PersistAndAck(currentOffset, publisherEvent);
                        return;
                    }
                }

                if (attempts == MaxRetryAttempts)
                    throw new ApplicationException(
                        $"Unable to process [{publisherEvent}] after [{MaxRetryAttempts}] - crashing projection process");
            }
            else
            {
                _log.Warning(
                    "Unsupported event [{0}] at offset [{1}] found by projector. Maybe this was tagged incorrectly?",
                    e.Event, e.Offset);

                // don't bother persisting here - move onto the next events in the buffer.
                Sender.Tell(ProjectionAck.Instance);
            }
        });

        Command<ProjectionStarting>(_ =>
        {
            _log.Info("Projection for Tag [{0}] is starting from Offset [{1}]", MessageTagger.ProductEventTag,
                CurrentState.LastOffset);
            Sender.Tell(ProjectionAck.Instance);
        });

        Command<ProjectionCompleted>(_ =>
        {
            _log.Info("Projection completed for Tag [{0}] at Offset [{1}]", MessageTagger.ProductEventTag,
                CurrentState.LastOffset);
        });

        Command<ProjectionFailed>(failed =>
        {
            var val = 0L;
            if (CurrentState.LastOffset is Sequence seq)
                val = seq.Value;
            _log.Error(failed.Cause, "Projection FAILED for Tag [{0}] at Offset [{1}]",
                MessageTagger.ProductEventTag, val);
            throw new ApplicationException("Projection failed due to error. See InnerException for details.",
                failed.Cause);
        });

        Command<SaveSnapshotSuccess>(success =>
        {
            // purge older snapshots and messages
            DeleteMessages(success.Metadata.SequenceNr);
            DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));
        });
    }

    private async Task<Akka.Util.Option<Exception>> TryProcess(IProductEvent pve,
        TimeSpan timeout)
    {
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            using var scope = _serviceProvider.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<CqrsSqlServerContext>();

            switch (pve)
            {
                case ProductCreated created:
                    await UpdateProductDefinitionAsync(created, context, cts.Token);
                    return Akka.Util.Option<Exception>.None;
                case ProductSold sold:
                    await UpdateProductSoldAsync(sold, context, cts.Token);
                    return Akka.Util.Option<Exception>.None;
                case ProductInventoryChanged changed:
                    await UpdateProductInventoryAsync(changed, context, cts.Token);
                    return Akka.Util.Option<Exception>.None;
            }

            return Akka.Util.Option<Exception>.None;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private async Task UpdateProductInventoryAsync(ProductInventoryChanged changed, CqrsSqlServerContext context, CancellationToken ctsToken)
    {
        var productListing = await context.Products.FindAsync([changed.ProductId], cancellationToken: ctsToken);
        if (productListing != null)
        {
            productListing.AllInventory += changed.Quantity;
            productListing.LastModified = changed.Timestamp;
            await context.SaveChangesAsync(ctsToken);
        }
    }

    private static async Task UpdateProductSoldAsync(ProductSold sold, CqrsSqlServerContext context, CancellationToken ctsToken)
    {
        var productListing = await context.Products.FindAsync([sold.ProductId], cancellationToken: ctsToken);
        if (productListing != null)
        {
            productListing.SoldUnits += sold.Order.Quantity;
            productListing.TotalRevenue += sold.TotalPrice;
            productListing.LastModified = sold.Order.Timestamp;
            await context.SaveChangesAsync(ctsToken);
        }
    }

    private static async Task UpdateProductDefinitionAsync(ProductCreated created, CqrsSqlServerContext context,
        CancellationToken ct = default)
    {
        var productListing = new ProductListing
        {
            ProductId = created.ProductId,
            ProductName = created.ProductName,
            Price = created.Price,
            Created = DateTime.UtcNow
        };

        var existing = await context.Products.FindAsync([created.ProductId], cancellationToken: ct);
        if (existing == null)
        {
            await context.AddAsync(productListing, ct);
            await context.SaveChangesAsync(ct);
        }
    }

    private void PersistAndAck(Offset currentOffset, IProductEvent pve)
    {
        var nextState = new MaterializedViewState(LastOffset: currentOffset);
        Persist(nextState, state =>
        {
            CurrentState = state;
            _log.Info("Successfully processed event [{0}] - projection state updated to [{1}]", pve,
                currentOffset);
            Sender.Tell(ProjectionAck.Instance);

            if (LastSequenceNr % 10 == 0)
            {
                SaveSnapshot(CurrentState);
            }
        });
    }
}