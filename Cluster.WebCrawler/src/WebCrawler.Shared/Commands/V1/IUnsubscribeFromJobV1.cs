using Akka.Actor;
using WebCrawler.Shared.State;

namespace WebCrawler.Shared.Commands.V1
{
    public interface IUnsubscribeFromJobV1
    {
        CrawlJob Job { get; }
        IActorRef Subscriber { get; }
    }
}