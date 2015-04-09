using System;
using System.Linq;
using Akka.Actor;
using Akka.Routing;
using Akka.Util.Internal;
using WebCrawler.Messages.Commands;
using WebCrawler.Messages.State;

namespace WebCrawler.Web.Actors
{
    /// <summary>
    /// Actor responsible for processing commands
    /// </summary>
    public class CommandProcessor : ReceiveActor
    {
        #region Messages

        public class AttemptCrawl
        {
            public AttemptCrawl(string rawStr)
            {
                RawStr = rawStr;
            }

            public string RawStr { get; private set; }
        }

        public class BadCrawlAttempt
        {
            public BadCrawlAttempt(string rawStr, string message)
            {
                Message = message;
                RawStr = rawStr;
            }

            public string RawStr { get; private set; }

            public string Message { get; private set; }
        }

        #endregion

        protected readonly IActorRef CommandRouter;

        public CommandProcessor(IActorRef commandRouter)
        {
            CommandRouter = commandRouter;
            Receives();
        }

        private void Receives()
        {
            Receive<AttemptCrawl>(attempt =>
            {
                if (Uri.IsWellFormedUriString(attempt.RawStr, UriKind.Absolute))
                {
                    var startJob = new StartJob(new CrawlJob(new Uri(attempt.RawStr, UriKind.Absolute), true), Sender);
                    CommandRouter.Tell(startJob);
                    CommandRouter.Ask<Routees>(new GetRoutees()).ContinueWith(tr =>
                    {
                        var grrr =
                            new SignalRActor.DebugCluster(string.Format("{0} has {1} routees: {2}", CommandRouter,
                                tr.Result.Members.Count(),
                                string.Join(",",
                                    tr.Result.Members.Select(
                                        y => y.ToString()))));

                        return grrr;
                    }).PipeTo(Sender);
                    Sender.Tell(startJob);
                }
                else
                {
                    Sender.Tell(new BadCrawlAttempt(attempt.RawStr, "Not an absolute URI"));
                }
            });
        }
    }
}