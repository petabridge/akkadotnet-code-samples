using Akka.Actor;

namespace WebCrawler.Web.Actors
{
    /// <summary>
    /// Static class used to work around weird SignalR constructors
    /// 
    /// (need to learn how to wire this up properly in signalr)
    /// </summary>
    public static class SystemActors
    {
        public static IActorRef SignalRActor = ActorRefs.Nobody;

        public static IActorRef CommandProcessor = ActorRefs.Nobody;
    }
}