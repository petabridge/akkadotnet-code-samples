using Bogus;

namespace LongRunningTasks;

public static class FakeDataGenerator
{
    public static List<CustomerRecord> Generate(int count)
    {
        var faker = new Faker<CustomerRecord>()
            .RuleFor(c => c.Id, f => f.IndexFaker + 1)
            .RuleFor(c => c.Name, f => f.Name.FullName())
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.Address, f => f.Address.FullAddress())
            .RuleFor(c => c.OrderCount, f => f.Random.Int(0, 100))
            .RuleFor(c => c.TotalSpent, f => f.Finance.Amount(0, 10000));

        return faker.Generate(count);
    }
}
