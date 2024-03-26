using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Bogus;
using CqrsSqlServer.Shared.Commands;
using CqrsSqlServer.Shared.Sharding;

namespace CqrsSqlServer.Shared;

public class FakeDataGenerator
{
    private readonly Faker<CreateProduct> _productGenerator;

    public FakeDataGenerator()
    {
        Randomizer.Seed = new Random(123456);
        _productGenerator = new Faker<CreateProduct>()
            .CustomInstantiator(f =>
            {
                var count = f.Random.Int(0, 3);
                return new CreateProduct(
                    f.Random.Guid().ToString(),
                    f.Commerce.ProductName(),
                    f.Finance.Amount(min: 1, max: 1000),
                    f.Random.Int(1, 1000),
                    count == 0 ? Array.Empty<string>() : f.Commerce.Categories(count)
                );
            });
    }

    public void Generate(ActorSystem system, IActorRegistry registry, int count)
    {
        var log = Logging.GetLogger(system, nameof(FakeDataGenerator));
        var shardRegion = registry.Get<ProductMarker>();
        
        GenerateAsync(count, shardRegion, log)
            .ContinueWith(t =>
            {
                if (!t.IsCompletedSuccessfully)
                {
                    log.Error(t.Exception, "Failed to generate fake data");
                }
            })
            .ConfigureAwait(false);
    }

    private async Task GenerateAsync(int count, IActorRef shardRegion, ILoggingAdapter log)
    {
        log.Info($"Generating {count} fake data");
        var items = new List<CreateProduct>();
        var total = 0;
        foreach (var _ in Enumerable.Range(0, count))
        {
            var item = _productGenerator.Generate();
            items.Add(item);
            var response = await shardRegion.Ask<ProductCommandResponse>(item, TimeSpan.FromSeconds(3));
            if (!response.Success)
            {
                log.Error(response.Message);
            }
            else
            {
                total++;
            }
        }

        var purchaseCount = count * 5;
        var rnd = Randomizer.Seed;
        var purchaseTotal = 0;
        var twoMinutes = TimeSpan.FromMinutes(2);
        var rollBackMinutes = twoMinutes * (purchaseCount + 1);
        var now = DateTime.UtcNow - rollBackMinutes;
        foreach (var _ in Enumerable.Range(0, purchaseCount))
        {
            var item = items[rnd.Next(0, items.Count)];
            var newOrder = new ProductOrder(Guid.NewGuid().ToString(), item.ProductId, rnd.Next(1, 100), now);
            now += twoMinutes;
            var createOrderCommand = new PurchaseProduct(newOrder);
            var response = await shardRegion.Ask<ProductCommandResponse>(createOrderCommand, TimeSpan.FromSeconds(3));
            if (!response.Success)
            {
                log.Error(response.Message);
            }
            else
            {
                purchaseTotal++;
            }
        }
        log.Info($"Generated {total} fake data and {purchaseTotal} fake purchases");
    }
}