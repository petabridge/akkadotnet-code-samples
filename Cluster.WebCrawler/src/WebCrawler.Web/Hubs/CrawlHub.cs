using System;
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
            Clients.All.writeStatus(string.Format("[{0}]({1}) - {2} ({3}) [{4} elapsed]", DateTime.UtcNow, update.Job, update.Stats, update.Status, update.Elapsed));
        }

        public void CrawlFailed(string reason)
        {
            Clients.All.writeStatus(reason);
        }

        public void StartCrawl(string message)
        {
            SystemActors.SignalRActor.Tell(message);
        }
    }
}