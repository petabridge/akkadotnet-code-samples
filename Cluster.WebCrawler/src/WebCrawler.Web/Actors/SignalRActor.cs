using Akka.Actor;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.SqlServer.Server;
using WebCrawler.Messages.Commands;
using WebCrawler.Messages.State;
using WebCrawler.Web.Hubs;

namespace WebCrawler.Web.Actors
{
    /// <summary>
    /// Actor used to wrap a signalr hub
    /// </summary>
    public class SignalRActor : ReceiveActor
    {
        #region Messages

        public class DebugCluster
        {
            public DebugCluster(string message)
            {
                Message = message;
            }

            public string Message { get; private set; }
        }

        #endregion

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

            Receive<JobStatusUpdate>(status =>
            {
                _hub.PushStatus(status);
            });

            Receive<StartJob>(start =>
            {
                _hub.WriteRawMessage(string.Format("Starting crawl of {0}", start.Job.Root.ToString()));
            });

            Receive<DebugCluster>(debug =>
            {
                _hub.WriteRawMessage(string.Format("DEBUG: {0}", debug.Message));
            });
        }

        protected override void PreStart()
        {
            var hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
            _hub = hubManager.ResolveHub("crawlHub") as CrawlHub;
        }


    }
}