using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Routing;
using WebCrawler.Shared.Config;
using WebCrawler.Web.Actors;

namespace WebCrawler.Web
{
    public static class AkkaStartupTasks
    {
        public static ActorSystem StartAkka()
        {
            var config = HoconLoader.ParseConfig("web.hocon");
            SystemActors.ActorSystem = ActorSystem.Create("webcrawler", config.BootstrapFromDocker());
            var router = SystemActors.ActorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "tasker");
            var processor = SystemActors.CommandProcessor = SystemActors.ActorSystem.ActorOf(Props.Create(() => new CommandProcessor(router)),
                "commands");
            SystemActors.SignalRActor = SystemActors.ActorSystem.ActorOf(Props.Create(() => new SignalRActor(processor)), "signalr");
            return SystemActors.ActorSystem;
        }
    }
}