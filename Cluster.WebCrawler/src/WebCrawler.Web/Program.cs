using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebCrawler.Web.Actors;

namespace WebCrawler.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                var wait = CoordinatedShutdown.Get(SystemActors.ActorSystem).Run(CoordinatedShutdown.ClrExitReason.Instance);
                await host.StopAsync(TimeSpan.FromSeconds(10));
                await wait;

            };

                host.Run();
                SystemActors.ActorSystem?.WhenTerminated.Wait();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    };


}
