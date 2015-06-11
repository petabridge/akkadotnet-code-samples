using Akka.Actor;
using WebCrawler.Messages.State;

namespace WebCrawler.Messages.Commands.V1
{
    public interface IUnsubscribeFromJobV1
    {
        CrawlJob Job { get; }
        IActorRef Subscriber { get; }
    }
}