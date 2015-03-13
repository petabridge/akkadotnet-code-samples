using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace WebCrawler.Service
{
    class Program
    {
        static int Main(string[] args)
        {
            return (int)HostFactory.Run(x =>
            {
                x.SetServiceName("Crawler");
                x.SetDisplayName("Akka.NET Crawler");
                x.SetDescription("Akka.NET Cluster Demo - Web Crawler.");

                x.UseAssemblyInfoForServiceInfo();
                x.RunAsLocalSystem();
                x.StartAutomatically();
                //x.UseNLog();
                x.Service<CrawlerService>();
                x.EnableServiceRecovery(r => r.RestartService(1));
            });
        }
    }
}
