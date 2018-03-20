using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Microsoft.AspNetCore.SignalR;
using WebCrawler.Web.Actors;

namespace WebCrawler.Web.Hubs
{
    public class CrawlHub : Hub
    {
        public void StartCrawl(string message)
        {
            SystemActors.SignalRActor.Tell(message, ActorRefs.Nobody);
        }
    }
}
