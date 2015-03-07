using Akka.Actor;
using WebCrawler.Service.State;

namespace WebCrawler.Service.Messages
{
    /// <summary>
    /// Represents a downloaded <see cref="CrawlDocument"/>
    /// </summary>
    public class CompletedDocument
    {
        public CompletedDocument(CrawlDocument document, int numBytes, ActorRef completedBy)
        {
            CompletedBy = completedBy;
            NumBytes = numBytes;
            Document = document;
        }

        public CrawlDocument Document { get; private set; }

        public int NumBytes { get; private set; }

        public ActorRef CompletedBy { get; private set; }
    }
}
