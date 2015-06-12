using Akka.Actor;
using Topshelf;
using WebCrawler.TrackingService.Actors;
using WebCrawler.TrackingService.Actors.Downloads;

namespace WebCrawler.TrackingService
{
    public class TrackerService : ServiceControl
    {
        protected ActorSystem ClusterSystem;
        protected IActorRef ApiMaster;
        protected IActorRef DownloadMaster;
        

        public bool Start(HostControl hostControl)
        {
            ClusterSystem = ActorSystem.Create("webcrawler");
            ApiMaster = ClusterSystem.ActorOf(Props.Create(() => new ApiMaster()), "api");
            DownloadMaster = ClusterSystem.ActorOf(Props.Create(() => new DownloadsMaster()), "downloads");
            //ApiMaster.Tell(new StartJob(new CrawlJob(new Uri("http://www.rottentomatoes.com/", UriKind.Absolute), true), ghettoConsoleActor));
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            ClusterSystem.Shutdown();
            return true;
        }
    }
}
