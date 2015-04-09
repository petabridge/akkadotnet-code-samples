using System;
using Akka.Actor;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using WebCrawler.Messages.Commands;
using WebCrawler.Messages.State;
using WebCrawler.Web.Actors;

namespace WebCrawler.Web.Hubs
{
    [HubName("crawlHub")]
    public class CrawlHub : Hub
    {
        public void PushStatus(JobStatusUpdate update)
        {
            WriteMessage(string.Format("[{0}]({1}) - {2} ({3}) [{4} elapsed]", DateTime.UtcNow, update.Job, update.Stats, update.Status, update.Elapsed));
        }

        public void CrawlFailed(string reason)
        {
           WriteMessage(reason);
        }

        public void StartCrawl(string message)
        {
            SystemActors.SignalRActor.Tell(message, ActorRefs.Nobody);
        }

        internal void WriteRawMessage(string msg)
        {
            WriteMessage(msg);
        }

        internal static void WriteMessage(string message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<CrawlHub>();
            dynamic allClients = context.Clients.All.writeStatus(message);
        }
    }
}