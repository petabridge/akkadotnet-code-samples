using System.Collections.Generic;
using Akka.Actor;
using WebCrawler.Service.State;

namespace WebCrawler.Service.Actors
{
    /// <summary>
    /// Actor responsible for documenting the persistence of documents
    /// </summary>
    public class CompletedDownloadsTracker : ReceiveActor
    {
        private readonly HashSet<CrawlDocument> _recordedDocuments;



        public CompletedDownloadsTracker(HashSet<CrawlDocument> recordedDocuments)
        {
            _recordedDocuments = recordedDocuments;
        }
    }
}
