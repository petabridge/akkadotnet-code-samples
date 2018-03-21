// -----------------------------------------------------------------------
// <copyright file="TrackerService.cs" company="Petabridge, LLC">
//      Copyright (C) 2018 - 2018 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using WebCrawler.Shared.Config;
using WebCrawler.TrackerService.Actors;
using WebCrawler.TrackerService.Actors.Tracking;

namespace WebCrawler.TrackerService
{
    public class TrackerService
    {
        protected IActorRef ApiMaster;
        protected ActorSystem ClusterSystem;
        protected IActorRef DownloadMaster;

        public Task WhenTerminated => ClusterSystem.WhenTerminated;


        public bool Start()
        {
            var config = HoconLoader.ParseConfig("tracker.hocon");
            ClusterSystem = ActorSystem.Create("webcrawler", config.BootstrapFromDocker());
            ApiMaster = ClusterSystem.ActorOf(Props.Create(() => new ApiMaster()), "api");
            DownloadMaster = ClusterSystem.ActorOf(Props.Create(() => new DownloadsMaster()), "downloads");
            return true;
        }

        public Task Stop()
        {
            return CoordinatedShutdown.Get(ClusterSystem).Run();
        }
    }
}