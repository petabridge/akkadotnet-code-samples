using Akka.Actor;
using Akka.Event;
using Bogus;
using SqlSharding.Shared.Commands;

namespace SqlSharding.Shared;

public class FakeDataGenerator
{
    private readonly IActorRef _shardRegion;
    private readonly Faker<CreateProduct> _productGenerator;

    public FakeDataGenerator(IActorRef shardRegion)
    {
        _shardRegion = shardRegion;
        _productGenerator = new Faker<CreateProduct>()
            .UseSeed(123456)
            //.StrictMode(true)
            .CustomInstantiator(f =>
            {
                var count = f.Random.Int(0, 3);
                return new CreateProduct(
                    f.Random.Guid().ToString(),
                    f.Commerce.ProductName(),
                    f.Finance.Amount(min: 1, max: 100000),
                    f.Random.Int(1, 100000),
                    count == 0 ? Array.Empty<string>() : f.Commerce.Categories(count)
                );
            });
    }

    public async Task Generate(int count, ILoggingAdapter log)
    {
        log.Info($"Generating {count} fake data");
        var total = 0;
        foreach (var _ in Enumerable.Range(0, count))
        {
            var response = await _shardRegion.Ask<ProductCommandResponse>(_productGenerator.Generate(), TimeSpan.FromSeconds(3));
            if (!response.Success)
            {
                log.Error(response.Message);
            }
            else
            {
                total++;
            }
        }
        log.Info($"Generated {total} fake data");
    }
}