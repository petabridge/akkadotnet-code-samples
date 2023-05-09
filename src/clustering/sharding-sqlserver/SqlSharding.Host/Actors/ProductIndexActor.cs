using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using Akka.DependencyInjection;
using Akka.Event;
using Akka.Hosting;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Streams;
using Akka.Streams.Dsl;
using SqlSharding.Shared.Queries;
using SqlSharding.Shared.Serialization.Proto;
using SqlSharding.Shared.Sharding;
using FetchAllProductsResponse = SqlSharding.Shared.Queries.FetchAllProductsResponse;
using FetchProduct = SqlSharding.Shared.Queries.FetchProduct;
using ProductData = SqlSharding.Shared.ProductData;

namespace SqlSharding.Host.Actors;

/// <summary>
/// Uses Akka.Persistence.Query to index all actively maintained <see cref="ProductTotalsActor"/>.
/// </summary>
public sealed class ProductIndexActor : ReceiveActor
{
    private readonly ILoggingAdapter _logging = Context.GetLogger();
    private readonly IActorRef _shardRegion;
    private ImmutableDictionary<string, ProductData> _productIds = ImmutableDictionary<string, ProductData>.Empty;
    private ImmutableDictionary<string, (IActorRef producerController, bool hasDemand)> _producers = ImmutableDictionary<string, (IActorRef producerController, bool hasDemand)>.Empty;

    public ProductIndexActor(IRequiredActor<ProductMarker> requiredActor)
    {
        // GRAB ISERVICEPROVIDER
        var diResolver = DependencyResolver.For(Context.System); 
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

        Receive<FetchAllProducts>(f =>
        {
            Sender.Tell(new FetchAllProductsResponse(_productIds.Values.ToList()));
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
}