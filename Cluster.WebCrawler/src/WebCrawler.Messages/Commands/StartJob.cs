using Akka.Actor;
using WebCrawler.Messages.State;

namespace WebCrawler.Messages.Commands
{
    /// <summary>
    /// Launch a new <see cref="CrawlJob"/>
    /// </summary>
    public class StartJob
    {
        public StartJob(CrawlJob job, ActorRef requestor)
        {
            Requestor = requestor;
            Job = job;
        }

        public CrawlJob Job { get; private set; }

        public ActorRef Requestor { get; private set; }
    }

    /// <summary>
    /// Kill a running <see cref="CrawlJob"/>
    /// </summary>
    public class StopJob
    {
        public StopJob(CrawlJob job, ActorRef requestor)
        {
            Requestor = requestor;
            Job = job;
        }

        public CrawlJob Job { get; private set; }

        public ActorRef Requestor { get; private set; }
    }
}