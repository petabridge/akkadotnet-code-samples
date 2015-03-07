using Akka.Actor;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using WebCrawler.Messages.State;
using WebCrawler.Web.Hubs;

namespace WebCrawler.Web.Actors
{
    /// <summary>
    /// Actor used to wrap a signalr hub
    /// </summary>
    public class SignalRActor : ReceiveActor
    {
        private CrawlHub _hub;

        public SignalRActor()
        {
            Receive<string>(str =>
            {
                SystemActors.CommandProcessor.Tell(new CommandProcessor.AttemptCrawl(str));
            });

            Receive<CommandProcessor.BadCrawlAttempt>(bad =>
            {
                _hub.CrawlFailed(string.Format("COULD NOT CRAWL {0}: {1}", bad.RawStr, bad.Message));
            });

            Receive<JobStatusMessage>(status =>
            {
                _hub.PushStatus(status);
            });
        }

        protected override void PreStart()
        {
            var hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
            _hub = hubManager.ResolveHub("crawlHub") as CrawlHub;
        }


    }
}