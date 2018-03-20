using Akka.Actor;
using WebCrawler.Shared.State;

namespace WebCrawler.Shared.Commands.V1
{
    public interface ISubscribeToJobV1
    {
        CrawlJob Job { get; }
        IActorRef Subscriber { get; }
    }
}