using Akka.Actor;
using Akka.Routing;
using WebCrawler.Messages.State;

namespace WebCrawler.Messages.Commands.V1
{
    public interface IStartJobV1 : IConsistentHashable
    {
        CrawlJob Job { get; }
        IActorRef Requestor { get; }
    }
}