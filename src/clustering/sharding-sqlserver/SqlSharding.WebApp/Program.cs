using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Phobos.Hosting;
using SqlSharding.Shared.Serialization;
using SqlSharding.Shared.Sharding;
using SqlSharding.Shared.Telemetry;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var akkaSection = builder.Configuration.GetSection("Akka");

// maps to environment variable Akka__ClusterIp
var hostName = akkaSection.GetValue<string>("ClusterIp", "localhost");

// maps to environment variable Akka__ClusterPort
var port = akkaSection.GetValue<int>("ClusterPort", 7918);

var seeds = akkaSection.GetValue<string[]>("ClusterSeeds", new []{ "akka.tcp://SqlSharding@localhost:7919" }).Select(Address.Parse)
    .ToArray();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddPhobosApm();
builder.Services.AddAkka("SqlSharding", (configurationBuilder, provider) =>
{
    configurationBuilder.WithRemoting(hostName, port)
        .AddAppSerialization()
        .WithClustering(new ClusterOptions()
            { Roles = new[] { "Web" }, SeedNodes = seeds })
        .WithShardRegionProxy<ProductMarker>("products", ProductActorProps.SingletonActorRole,
            new ProductMessageRouter())
        .WithActors((system, registry) =>
        {
            var proxyProps = system.ProductIndexProxyProps();
            registry.TryRegister<ProductIndexMarker>(system.ActorOf(proxyProps, "product-proxy"));
        })
        .WithPhobos(AkkaRunMode.AkkaCluster, configBuilder => configBuilder.WithTracing(t => t.SetTraceFilter(new OnlyActiveTracesFilter())));
});

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
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseAuthorization();

app.MapRazorPages();

app.Run();