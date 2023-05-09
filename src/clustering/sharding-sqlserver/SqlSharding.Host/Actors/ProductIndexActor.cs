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
using SqlSharding.Shared.Queries;
using SqlSharding.Shared.Sharding;
using FetchAllProductsResponse = SqlSharding.Shared.Queries.FetchAllProductsResponse;
using FetchProduct = SqlSharding.Shared.Queries.FetchProduct;
using ProductData = SqlSharding.Shared.ProductData;

namespace SqlSharding.Host.Actors;

/// <summary>
/// Uses Akka.Persistence.Query to index all actively maintained <see cref="ProductTotalsActor"/>.
/// </summary>
public sealed class ProductIndexActor : ReceiveActor, IWithTimers
{
    private readonly ILoggingAdapter _logging = Context.GetLogger();
    private const int MaxAttempts = 3;
    private readonly IActorRef _shardRegion;
    private ImmutableDictionary<string, ProductData> _productIds = ImmutableDictionary<string, ProductData>.Empty;
    private ImmutableDictionary<string, (IActorRef producerController, bool hasDemand)> _producers = ImmutableDictionary<string, (IActorRef producerController, bool hasDemand)>.Empty;

    private record ConsumerTerminated(string ProducerId);
    
    private record RetryFetchAllProducts(FetchAllProductsImpl OriginalRequest, int Attempts);

    public ProductIndexActor(IRequiredActor<ProductMarker> requiredActor)
    {
        _shardRegion = requiredActor.ActorRef;
        Receive<ProductFound>(found =>
        {
            _logging.Info("Found product [{0}]", found);
            _productIds = _productIds.Add(found.ProductId, ProductData.Empty);
            _shardRegion.Tell(new FetchProduct(found.ProductId));
        });

        Receive<FetchResult>(result =>
        {
            _logging.Info("Received product state for product [{0}]", result.State.ProductId);
            _productIds = _productIds.SetItem(result.State.ProductId, result.State.Data);
        });

        Receive<FetchAllProductsImpl>(f => { ReceiveFetchProducts(f, 1); });

        Receive<ProducerController.RequestNext<IFetchAllProductsProtocol>>(next =>
        {
            // signal that demand is available
            _producers = _producers.SetItem(next.ProducerId, (_producers[next.ProducerId].producerController, true));
        });

        Receive<RetryFetchAllProducts>(r =>
        {
            ReceiveFetchProducts(r.OriginalRequest, r.Attempts);
        });
        
        Receive<ConsumerTerminated>(t =>
        {
            if (_producers.TryGetValue(t.ProducerId, out var p))
            {
                Context.Stop(p.producerController);
                _producers = _producers.Remove(t.ProducerId);
            }
        });

        Receive<Done>(_ =>
        {
            // this should never happen
            throw new InvalidOperationException("SHOULD NOT REACH END OF ID STREAM");
        });

        Receive<Status.Failure>(f =>
        {
            _logging.Error(f.Cause, "Failed to read product from Akka.Persistence.Query");
            throw new InvalidOperationException("SHOULD NOT REACH END OF ID STREAM");
        });
    }

    private void ReceiveFetchProducts(FetchAllProductsImpl f, int attemptNo)
    {
        if (_producers.TryGetValue(f.ProducerId, out var t))
        {
            if (t.hasDemand)
            {
                // let the ProducerController perform sequencing
                t.producerController.Tell(new FetchAllProductsResponse(_productIds.Values.ToList()));
                
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
            var producerController = CreateProducerController(f.ProducerId, Sender);
            _producers = _producers.Add(f.ProducerId, (producerController, false));
            RetryRequest(f, attemptNo + 1);
        }
    }

    private void RetryRequest(FetchAllProductsImpl f, int currentAttempt)
    {
        if (currentAttempt > MaxAttempts)
        {
            // request failed
            _logging.Error("Failed to process FetchAllProducts for [{0}] afters [{1}] attempts", f.ProducerId, MaxAttempts);
        }
        else
        {
            Timers.StartSingleTimer(f.ProducerId, new RetryFetchAllProducts(f, currentAttempt), TimeSpan.FromSeconds(1));
        }
    }

    private readonly record struct ProductFound(string ProductId);

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
        query.PersistenceIds()
            .Where(c => c.StartsWith(ProductTotalsActor.TotalsEntityNameConstant))
            .Select(c =>
            {
                var splitPivot = c.IndexOf("-", StringComparison.Ordinal);
                return new ProductFound(c[(splitPivot + 1)..]);
            })
            .To(Sink.ActorRef<ProductFound>(Self, Done.Instance, ex => new Status.Failure(ex)))
            .Run(Context.Materializer());
    }
    
    private IActorRef CreateProducerController(string producerId, IActorRef requestor)
    {
        // 64KB chunks
        var producerControllerSettings = ProducerController.Settings.Create(Context.System) with { ChunkLargeMessagesBytes = 64 * 1024 };
        var producerControllerProps =
            ProducerController.Create<IFetchAllProductsProtocol>(Context, producerId, Option<Props>.None, producerControllerSettings);
        Context.WatchWith(requestor, new ConsumerTerminated(producerId));
        var producerController = Context.ActorOf(producerControllerProps, $"producer-controller-{producerId}");
        
        // register the consumer with the producer
        producerController.Tell(new ProducerController.RegisterConsumer<IFetchAllProductsProtocol>(requestor));
        
        // start production
        producerController.Tell(new ProducerController.Start<IFetchAllProductsProtocol>(Self));
        return producerController;
    }

    public ITimerScheduler Timers { get; set; } = null!;
}