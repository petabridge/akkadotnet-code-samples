using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Akka.Actor;
using Akka.Routing;
using WebCrawler.Web.Actors;
using System;

namespace WebCrawler.Web
{
    public class MvcApplication : HttpApplication
    {
        protected static ActorSystem ActorSystem;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ActorSystem = ActorSystem.Create("webcrawler");
            var router = ActorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "tasker");
            SystemActors.CommandProcessor = ActorSystem.ActorOf(Props.Create(() => new CommandProcessor(router)),
                "commands");
            SystemActors.SignalRActor = ActorSystem.ActorOf(Props.Create(() => new SignalRActor()), "signalr");
        }

        protected void Application_End()
        {
            CoordinatedShutdown.Get(ActorSystem).Run().Wait(TimeSpan.FromSeconds(5));
        }
    }
}
