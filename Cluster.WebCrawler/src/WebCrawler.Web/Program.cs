using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebCrawler.Web.Actors;

namespace WebCrawler.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);

            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                var wait = CoordinatedShutdown.Get(SystemActors.ActorSystem).Run();
                await host.StopAsync(TimeSpan.FromSeconds(10));
                await wait;
            };

           
            host.Run();
            SystemActors.ActorSystem?.WhenTerminated.Wait();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();
    }
}
