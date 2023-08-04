using System.Collections.Immutable;
using Akka.Actor;
using Akka.Delivery;
using Akka.Event;
using Akka.Hosting;
using Akka.Persistence.Query;
using Akka.Persistence.Sql.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Util;
using SqlSharding.Shared;
using SqlSharding.Shared.Events;
using SqlSharding.Shared.Queries;
using SqlSharding.Shared.Sharding;

namespace SqlSharding.Host.Actors;

public class WarningEventIndexActor : ReceiveActor, IWithTimers
{
    private readonly ILoggingAdapter _logging = Context.GetLogger();
    private const int MaxAttempts = 3;
    private readonly IActorRef _shardRegion;
    private ImmutableDictionary<string, WarningEventData> _warningEvents = ImmutableDictionary<string, WarningEventData>.Empty;
    private ImmutableDictionary<string, (IActorRef producerController, bool hasDemand)> _producers = ImmutableDictionary<string, (IActorRef producerController, bool hasDemand)>.Empty;

    private record ConsumerTerminated(string ProducerId);
    
    private record RetryFetchWarnings(FetchWarningEventsImpl OriginalRequest, int Attempts);

    public WarningEventIndexActor(IRequiredActor<ProductMarker> requiredActor)
    {
        _shardRegion = requiredActor.ActorRef;
        Receive<WarningEventFound>(found =>
        {
            _logging.Info("Found warning event [{0}]", found.ProductId);

            if (!_warningEvents.TryGetValue(found.ProductId, out var alertData))
            {
                alertData = WarningEventData.Empty;
                _shardRegion.Tell(new FetchProduct(found.ProductId));
            }
            
            alertData = alertData with { Warnings = alertData.Warnings.Add(found.Data) };
            
            _warningEvents = _warningEvents.SetItem(found.ProductId, alertData);
        });

        Receive<FetchResult>(result =>
        {
            var productId = result.State.ProductId;
            _logging.Info("Received product state for sold product [{0}]", productId);
            _warningEvents = _warningEvents.SetItem(productId, _warningEvents[productId] with { ProductData = result.State.Data });
        });

        Receive<FetchWarningEventsImpl>(f => ReceiveFetchWarningEvents(f, 1));
        
        Receive<ProducerController.RequestNext<IFetchWarningEventsProtocol>>(next =>
        {
            // signal that demand is available
            _producers = _producers.SetItem(next.ProducerId, (_producers[next.ProducerId].producerController, true));
        });

        Receive<RetryFetchWarnings>(r =>
        {
            ReceiveFetchWarningEvents(r.OriginalRequest, r.Attempts);
        });
        
        Receive<ConsumerTerminated>(t =>
        {
            if (_producers.TryGetValue(t.ProducerId, out var p))
            {
                Context.Stop(p.producerController);
                _producers = _producers.Remove(t.ProducerId);
            }
        });

        // this should never happen
        Receive<Done>(_ => throw new InvalidOperationException("SHOULD NOT REACH END OF ID STREAM"));

        Receive<Status.Failure>(f =>
        {
            _logging.Error(f.Cause, "Failed to read warning events from Akka.Persistence.Query");
            throw new InvalidOperationException("SHOULD NOT REACH END OF ID STREAM");
        });
    }

    private void ReceiveFetchWarningEvents(FetchWarningEventsImpl f, int attemptNo)
    {
        if (_producers.TryGetValue(f.ProducerId, out var t))
        {
            if (t.hasDemand)
            {
                // let the ProducerController perform sequencing
                t.producerController.Tell(new FetchWarningEventsResponse(_warningEvents.Values.ToList()));
                
                // signal that demand has been met (for now)
                _producers = _producers.SetItem(f.ProducerId, (t.producerController, false));
                Timers.Cancel(f.ProducerId); // if there was a scheduled retry pending - client beat us to it
            }
            else
            {
                // retry in 1 second
                RetryRequest(f, attemptNo + 1);
            }
        }
        else
        {
            // need to start ProducerController
            var producerController = CreateProducerController(f.ProducerId, f.ConsumerController);
            _producers = _producers.Add(f.ProducerId, (producerController, false));
            RetryRequest(f, attemptNo + 1);
        }
    }
    
    private void RetryRequest(FetchWarningEventsImpl f, int currentAttempt)
    {
        if (currentAttempt > MaxAttempts)
        {
            // request failed
            _logging.Error("Failed to process FetchWarningEvents for [{0}] afters [{1}] attempts", f.ProducerId, MaxAttempts);
        }
        else
        {
            Timers.StartSingleTimer(f.ProducerId, new RetryFetchWarnings(f, currentAttempt), TimeSpan.FromSeconds(1));
        }
    }

    private readonly record struct WarningEventFound(string ProductId, ProductInventoryWarningEvent Data);

    private sealed class Done
    {
        public static readonly Done Instance = new();
        private Done()
        {
        }
    }

    protected override void PreStart()
    {
        /*
         * Kicks off an Akka.Persistence.Query instance that will continuously
         */
        var query = Context.System.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
        query.EventsByTag(MessageTagger.WarningEventTag, Offset.Sequence(0))
            .Where(e => e.PersistenceId.StartsWith(ProductTotalsActor.TotalsEntityNameConstant))
            .Select(e =>
            {
                var splitPivot = e.PersistenceId.IndexOf("-", StringComparison.Ordinal);
                return new WarningEventFound(e.PersistenceId[(splitPivot + 1)..], (ProductInventoryWarningEvent)e.Event);
            })
            .To(Sink.ActorRef<WarningEventFound>(Self, Done.Instance, ex => new Status.Failure(ex)))
            .Run(Context.Materializer());
    }
    
    private IActorRef CreateProducerController(string producerId, IActorRef requestor)
    {
        // 64KB chunks
        var producerControllerSettings = ProducerController.Settings.Create(Context.System) with { ChunkLargeMessagesBytes = 1024 };
        var producerControllerProps =
            ProducerController.Create<IFetchWarningEventsProtocol>(Context, producerId, Option<Props>.None, producerControllerSettings);
        Context.WatchWith(requestor, new ConsumerTerminated(producerId));
        var producerController = Context.ActorOf(producerControllerProps, $"warning-producer-controller-{producerId}");
        
        // register the consumer with the producer
        producerController.Tell(new ProducerController.RegisterConsumer<IFetchWarningEventsProtocol>(requestor));
        
        // start production
        producerController.Tell(new ProducerController.Start<IFetchWarningEventsProtocol>(Self));
        return producerController;
    }

    public ITimerScheduler Timers { get; set; } = null!;

}