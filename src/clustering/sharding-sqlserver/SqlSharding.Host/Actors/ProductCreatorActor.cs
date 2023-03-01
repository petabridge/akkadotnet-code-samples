using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.Streams;
using Akka.Streams.Dsl;
using SqlSharding.Shared.Commands;
using SqlSharding.Shared.Sharding;

namespace SqlSharding.Host.Actors;

public class ProductCreatorActor : ReceiveActor
{
    private class PopulateProducts
    {
        public static readonly PopulateProducts Instance = new();

        private PopulateProducts()
        {
        }
    }

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly int _totalProducts;
    private readonly IActorRef _productsShardRegion;

    public ProductCreatorActor(int totalProducts, IRequiredActor<ProductMarker> shardRegion)
    {
        _totalProducts = totalProducts;
        _productsShardRegion = shardRegion.ActorRef;

        Receive<PopulateProducts>(_ =>
        {
            var source = Source.From(Enumerable.Range(1, _totalProducts))
                .Select(i => new CreateProduct(i.ToString(), "product-{i}", 10.0m, 100))
                .SelectAsync(30,
                    async product =>
                        await _productsShardRegion.Ask<ProductCommandResponse>(product, TimeSpan.FromSeconds(30)))
                .RunWith(Sink.ActorRef<ProductCommandResponse>(Self, Done.Instance), Context.System.Materializer());
        });

        Receive<ProductCommandResponse>(pcr =>
        {
            _log.Info("Created product {0}", pcr.ProductId);
        });
        
        Receive<Status.Failure>(f =>
        {
            _log.Error(f.Cause, "Failed to create product");
        });
    }

    protected override void PreStart()
    {
        Self.Tell(PopulateProducts.Instance);
    }
}