using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using Akka.Event;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Streams;
using Akka.Streams.Dsl;
using SqlSharding.Shared.Queries;

namespace SqlSharding.Host.Actors;

/// <summary>
/// Uses Akka.Persistence.Query to index all actively maintained <see cref="ProductTotalsActor"/>.
/// </summary>
public sealed class ProductIndexActor : ReceiveActor
{
    private readonly ILoggingAdapter _logging = Context.GetLogger();
    private ImmutableHashSet<string> _productIds = ImmutableHashSet<string>.Empty;

    public ProductIndexActor()
    {
        Receive<ProductFound>(found =>
        {
            _logging.Info("Found product [{0}]", found);
            _productIds = _productIds.Add(found.ProductId);
        });

        Receive<FetchAllProducts>(f =>
        {
            Sender.Tell(new FetchAllProductsResponse(_productIds.ToList()));
        });

        Receive<Done>(_ =>
        {
            // this should never happen
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
            .To(Sink.ActorRef<ProductFound>(Self, Done.Instance))
            .Run(Context.Materializer());
    }
}