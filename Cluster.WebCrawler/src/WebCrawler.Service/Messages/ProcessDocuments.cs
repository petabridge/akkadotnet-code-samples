using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using WebCrawler.Service.State;

namespace WebCrawler.Service.Messages
{
    /// <summary>
    /// Message class used to confirm which documents are available for processing.
    /// </summary>
    public class ProcessDocuments
    {
        public ProcessDocuments(IList<CrawlDocument> documents, IActorRef assigned)
        {
            Assigned = assigned;
            Documents = documents;
        }

        public IList<CrawlDocument> Documents { get; private set; }

        public int HtmlDocs { get { return Documents.Count(x => !x.IsImage); } }

        public int Images { get { return Documents.Count(x => x.IsImage); } }

        /// <summary>
        /// Reference to the actor who should take on the cleared documents
        /// </summary>
        public IActorRef Assigned { get; private set; }
    }
}