using Akka.Actor;
using WebCrawler.Messages.State;

namespace WebCrawler.Messages.Commands
{
    /// <summary>
    /// Subscribe an actor to a given <see cref="CrawlJob"/>
    /// </summary>
    public class SubscribeToJob
    {
        public SubscribeToJob(CrawlJob job, IActorRef subscriber)
        {
            Subscriber = subscriber;
            Job = job;
        }

        public CrawlJob Job { get; private set; }

        public IActorRef Subscriber { get; private set; }
    }
}
