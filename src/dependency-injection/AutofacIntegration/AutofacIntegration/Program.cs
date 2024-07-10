using Akka.Hosting;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoFacIntegration;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args);

        #region Boilerplate code

        hostBuilder.ConfigureAppConfiguration((context, builder) =>
        {
            var env = context.HostingEnvironment;
            builder
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
        });

        hostBuilder.ConfigureLogging((context, logging) =>
        {
            logging.AddConsole();
            logging.AddDebug();
        });

        #endregion

        #region Autofac setup

        hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        hostBuilder.ConfigureContainer<ContainerBuilder>((context, builder) =>
        {
            builder.RegisterType<AutofacModule>();
        });

        #endregion

        hostBuilder.ConfigureServices((context, services) =>
        {
            #region Akka.Hosting setup

            services.AddAkka("sample-system", (builder, provider) =>
            {
                builder.StartActors((system, registry, resolver) =>
                {
                    var echoActor = system.ActorOf(resolver.Props<EchoActor>(), "echo-actor");
                    registry.Register<EchoActor>(echoActor);

                    var testActor = system.ActorOf(resolver.Props<TestActor>(), "test-actor");
                    registry.Register<TestActor>(testActor);
                });
            });

            #endregion
        });

        await hostBuilder.RunConsoleAsync();
    }
}