using System;
using Akka.Actor;
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

        protected readonly ActorRef CommandRouter;

        public CommandProcessor(ActorRef commandRouter)
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
                    CommandRouter.Tell(new StartJob(new CrawlJob(new Uri(attempt.RawStr, UriKind.Absolute), true),
                        Sender));
                }
                else
                {
                    Sender.Tell(new BadCrawlAttempt(attempt.RawStr, "Not an absolute URI"));
                }
            });
        }
    }
}