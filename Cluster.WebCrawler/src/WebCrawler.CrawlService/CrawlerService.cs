using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using WebCrawler.Shared.Config;

namespace WebCrawler.CrawlService
{
    public class CrawlerService
    {
        protected ActorSystem ClusterSystem;

        public Task WhenTerminated => ClusterSystem.WhenTerminated;


        public bool Start()
        {
            var config = HoconLoader.ParseConfig("crawler.hocon");
            ClusterSystem = ActorSystem.Create("webcrawler", config.BootstrapFromDocker());
            return true;
        }

        public Task Stop()
        {
            return CoordinatedShutdown.Get(ClusterSystem).Run(CoordinatedShutdown.ClrExitReason.Instance);
        }
    }
}
