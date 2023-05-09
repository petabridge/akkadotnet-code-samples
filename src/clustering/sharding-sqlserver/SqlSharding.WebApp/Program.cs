using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using SqlSharding.Shared.Serialization;
using SqlSharding.Shared.Sharding;
using SqlSharding.WebApp.Actors;
using SqlSharding.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var akkaSection = builder.Configuration.GetSection("Akka");

// maps to environment variable Akka__ClusterIp
var hostName = akkaSection.GetValue<string>("ClusterIp", "localhost");

// maps to environment variable Akka__ClusterPort
var port = akkaSection.GetValue<int>("ClusterPort", 7918);

var seeds = akkaSection.GetValue<string[]>("ClusterSeeds", new []{ "akka.tcp://SqlSharding@localhost:7918" })
    .ToArray();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddAkka("SqlSharding", (configurationBuilder, provider) =>
{
    configurationBuilder.WithRemoting(hostName, port)
        .AddAppSerialization()
        .WithClustering(new ClusterOptions()
            { Roles = new[] { "Web" }, SeedNodes = seeds })
        .WithShardRegionProxy<ProductMarker>("products", ProductActorProps.SingletonActorRole,
            new ProductMessageRouter())
        .WithSingletonProxy<ProductIndexMarker>("product-proxy",
            new ClusterSingletonOptions() { Role = ProductActorProps.SingletonActorRole })
        .WithActors((system, registry, resolver) =>
        {
            var consumerProps = resolver.Props<FetchAllProductsConsumer>();
            var consumerActor = system.ActorOf(consumerProps, "fetch-all-products-consumer");
            registry.Register<FetchAllProductsConsumer>(consumerActor);
        });
});
builder.Services.AddSingleton<IProductsResolver, ActorProductsResolver>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();