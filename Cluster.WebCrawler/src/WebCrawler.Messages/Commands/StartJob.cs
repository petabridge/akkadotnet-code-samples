using Akka.Actor;
using Akka.Routing;
using WebCrawler.Messages.State;

namespace WebCrawler.Messages.Commands
{
    /// <summary>
    /// Launch a new <see cref="CrawlJob"/>
    /// </summary>
    public class StartJob : IConsistentHashable
    {
        public StartJob(CrawlJob job, IActorRef requestor)
        {
            Requestor = requestor;
            Job = job;
        }

        public CrawlJob Job { get; private set; }

        public IActorRef Requestor { get; private set; }
        public object ConsistentHashKey { get { return Job.Root.OriginalString; } }
    }

    /// <summary>
    /// Kill a running <see cref="CrawlJob"/>
    /// </summary>
    public class StopJob
    {
        public StopJob(CrawlJob job, IActorRef requestor)
        {
            Requestor = requestor;
            Job = job;
        }

        public CrawlJob Job { get; private set; }

        public IActorRef Requestor { get; private set; }
    }
}