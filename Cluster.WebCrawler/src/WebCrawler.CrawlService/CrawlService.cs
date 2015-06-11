using Akka.Actor;
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
            ClusterSystem.Shutdown();
            return true;
        }
    }
}
