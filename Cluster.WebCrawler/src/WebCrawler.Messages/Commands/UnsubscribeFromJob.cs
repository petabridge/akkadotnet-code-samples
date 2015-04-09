using Akka.Actor;
using WebCrawler.Messages.State;

namespace WebCrawler.Messages.Commands
{
    /// <summary>
    /// Unsuscribe an actor from a given <see cref="CrawlJob"/>
    /// </summary>
    public class UnsubscribeFromJob
    {
        public UnsubscribeFromJob(CrawlJob job, IActorRef subscriber)
        {
            Subscriber = subscriber;
            Job = job;
        }

        public CrawlJob Job { get; private set; }

        public IActorRef Subscriber { get; private set; }
    }
}