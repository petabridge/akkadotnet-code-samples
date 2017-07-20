using Akka.Actor;
using System;
using Topshelf;

namespace WebCrawler.CrawlService
{
    class CrawlService : ServiceControl
    {
        public bool Start(HostControl hostControl)
        {
            ClusterSystem = ActorSystem.Create("webcrawler");
            return true;
        }

        protected ActorSystem ClusterSystem { get; set; }

        public bool Stop(HostControl hostControl)
        {
            CoordinatedShutdown.Get(ClusterSystem).Run().Wait(TimeSpan.FromSeconds(5));
            return true;
        }
    }
}
