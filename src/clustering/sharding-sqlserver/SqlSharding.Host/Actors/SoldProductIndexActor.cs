using System.Collections.Immutable;
using Akka.Actor;
using Akka.Delivery;
using Akka.Event;
using Akka.Hosting;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Util;
using SqlSharding.Shared;
using SqlSharding.Shared.Events;
using SqlSharding.Shared.Queries;
using SqlSharding.Shared.Sharding;

namespace SqlSharding.Host.Actors;

public class SoldProductIndexActor : ReceiveActor, IWithTimers
{
    private readonly ILoggingAdapter _logging = Context.GetLogger();
    private const int MaxAttempts = 3;
    private readonly IActorRef _shardRegion;
    private ImmutableDictionary<string, ProductsSoldData> _soldProducts = ImmutableDictionary<string, ProductsSoldData>.Empty;
    private ImmutableDictionary<string, (IActorRef producerController, bool hasDemand)> _producers = ImmutableDictionary<string, (IActorRef producerController, bool hasDemand)>.Empty;

    private record ConsumerTerminated(string ProducerId);
    
    private record RetryFetchProducts(FetchSoldProductsImpl OriginalRequest, int Attempts);

    public SoldProductIndexActor(IRequiredActor<ProductMarker> requiredActor)
    {
        _shardRegion = requiredActor.ActorRef;
        Receive<SoldProductFound>(found =>
        {
            _logging.Info("Found sold product [{0}]", found.ProductId);

            if (!_soldProducts.TryGetValue(found.ProductId, out var soldData))
            {
                soldData = ProductsSoldData.Empty;
                _shardRegion.Tell(new FetchProduct(found.ProductId));
            }
            
            soldData = soldData with { Invoices = soldData.Invoices.Add(found.Data) };
            
            _soldProducts = _soldProducts.SetItem(found.ProductId, soldData);
        });

        Receive<FetchResult>(result =>
        {
            var productId = result.State.ProductId;
            _logging.Info("Received product state for sold product [{0}]", productId);
            _soldProducts = _soldProducts.SetItem(productId, _soldProducts[productId] with { ProductData = result.State.Data });
        });

        Receive<FetchSoldProductsImpl>(f => ReceiveFetchSoldProducts(f, 1));
        
        Receive<ProducerController.RequestNext<IFetchSoldProductsProtocol>>(next =>
        {
            // signal that demand is available
            _producers = _producers.SetItem(next.ProducerId, (_producers[next.ProducerId].producerController, true));
        });

        Receive<RetryFetchProducts>(r =>
        {
            ReceiveFetchSoldProducts(r.OriginalRequest, r.Attempts);
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
            _logging.Error(f.Cause, "Failed to read sold product from Akka.Persistence.Query");
            throw new InvalidOperationException("SHOULD NOT REACH END OF ID STREAM");
        });
    }

    private void ReceiveFetchSoldProducts(FetchSoldProductsImpl f, int attemptNo)
    {
        if (_producers.TryGetValue(f.ProducerId, out var t))
        {
            if (t.hasDemand)
            {
                // let the ProducerController perform sequencing
                t.producerController.Tell(new FetchSoldProductsResponse(_soldProducts.Values.ToList()));
                
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
    
    private void RetryRequest(FetchSoldProductsImpl f, int currentAttempt)
    {
        if (currentAttempt > MaxAttempts)
        {
            // request failed
            _logging.Error("Failed to process FetchSoldProducts for [{0}] afters [{1}] attempts", f.ProducerId, MaxAttempts);
        }
        else
        {
            Timers.StartSingleTimer(f.ProducerId, new RetryFetchProducts(f, currentAttempt), TimeSpan.FromSeconds(1));
        }
    }

    private readonly record struct SoldProductFound(string ProductId, ProductSold Data);

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
        query.EventsByTag(MessageTagger.SoldEventTag, Offset.Sequence(0))
            .Where(e => e.PersistenceId.StartsWith(ProductTotalsActor.TotalsEntityNameConstant))
            .Select(e =>
            {
                var splitPivot = e.PersistenceId.IndexOf("-", StringComparison.Ordinal);
                return new SoldProductFound(e.PersistenceId[(splitPivot + 1)..], (ProductSold)e.Event);
            })
            .To(Sink.ActorRef<SoldProductFound>(Self, Done.Instance, ex => new Status.Failure(ex)))
            .Run(Context.Materializer());
    }
    
    private IActorRef CreateProducerController(string producerId, IActorRef requestor)
    {
        // 64KB chunks
        var producerControllerSettings = ProducerController.Settings.Create(Context.System) with { ChunkLargeMessagesBytes = 1024 };
        var producerControllerProps =
            ProducerController.Create<IFetchSoldProductsProtocol>(Context, producerId, Option<Props>.None, producerControllerSettings);
        Context.WatchWith(requestor, new ConsumerTerminated(producerId));
        var producerController = Context.ActorOf(producerControllerProps, $"sold-producer-controller-{producerId}");
        
        // register the consumer with the producer
        producerController.Tell(new ProducerController.RegisterConsumer<IFetchSoldProductsProtocol>(requestor));
        
        // start production
        producerController.Tell(new ProducerController.Start<IFetchSoldProductsProtocol>(Self));
        return producerController;
    }

    public ITimerScheduler Timers { get; set; } = null!;

}