using Akka.Actor;
using Microsoft.AspNetCore.SignalR;
using WebCrawler.Shared.Commands.V1;
using WebCrawler.Web.Hubs;

namespace WebCrawler.Web.Actors
{
    /// <summary>
    /// Actor used to wrap a signalr hub
    /// </summary>
    public class SignalRActor : ReceiveActor, IWithUnboundedStash
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

        public class SetHub : INoSerializationVerificationNeeded
        {
            public SetHub(CrawlHubHelper hub)
            {
                Hub = hub;
            }
            public CrawlHubHelper Hub { get; }
        }

        #endregion

        private readonly IActorRef _commandProcessor;

        private CrawlHubHelper _hub;

        public SignalRActor(IActorRef commandProcessor)
        {
            _commandProcessor = commandProcessor;

            WaitingForHub();
        }

        private void HubAvailable()
        {
            Receive<string>(str => { _commandProcessor.Tell(new CommandProcessor.AttemptCrawl(str)); });

            Receive<CommandProcessor.BadCrawlAttempt>(bad =>
            {
                _hub.CrawlFailed(string.Format("COULD NOT CRAWL {0}: {1}", bad.RawStr, bad.Message));
            });

            Receive<IStatusUpdateV1>(status => { _hub.PushStatus(status); });

            Receive<IStartJobV1>(start =>
            {
                _hub.WriteRawMessage(string.Format("Starting crawl of {0}", start.Job.Root.ToString()));
            });

            Receive<DebugCluster>(debug => { _hub.WriteRawMessage(string.Format("DEBUG: {0}", debug.Message)); });
        }

        private void WaitingForHub()
        {
            Receive<SetHub>(h =>
            {
                _hub = h.Hub;
                Become(HubAvailable);
                Stash.UnstashAll();
            });

            ReceiveAny(_ => Stash.Stash());
        }


        public IStash Stash { get; set; }
    }
}