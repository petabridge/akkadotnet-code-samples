using System;
using Akka.Actor;
using Topshelf;
using WebCrawler.Messages.Commands;
using WebCrawler.Messages.State;
using WebCrawler.Service.Actors;
using WebCrawler.Service.Actors.Downloads;

namespace WebCrawler.Service
{
    public class GhettoConsoleActor : ReceiveActor
    {
        public GhettoConsoleActor()
        {
            Receive<JobStatusUpdate>(update => Console.WriteLine("[{0}]({1}) - {2} ({3}) [{4} elapsed]", DateTime.UtcNow, update.Job, update.Stats, update.Status, update.Elapsed));
        }
    }

    public class CrawlerService : ServiceControl
    {
        protected ActorSystem ClusterSystem;
        protected IActorRef ApiMaster;
        protected IActorRef DownloadMaster;
        

        public bool Start(HostControl hostControl)
        {
            ClusterSystem = ActorSystem.Create("webcrawler");
            ApiMaster = ClusterSystem.ActorOf(Props.Create(() => new ApiMaster()), "api");
            DownloadMaster = ClusterSystem.ActorOf(Props.Create(() => new DownloadsMaster()), "downloads");
            var ghettoConsoleActor = ClusterSystem.ActorOf(Props.Create(() => new GhettoConsoleActor()));
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
