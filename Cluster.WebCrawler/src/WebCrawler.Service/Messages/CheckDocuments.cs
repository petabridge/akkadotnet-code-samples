using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using WebCrawler.Service.State;

namespace WebCrawler.Service.Messages
{
    /// <summary>
    /// Message class used to check to see if any of the listed
    /// <see cref="CrawlDocument"/>s have are currently being processed
    /// or have previously been processed.
    /// </summary>
    public class CheckDocuments
    {
        public CheckDocuments(IList<CrawlDocument> documents, IActorRef requestor, TimeSpan? estimatedCrawlTime)
        {
            EstimatedCrawlTime = estimatedCrawlTime;
            Requestor = requestor;
            Documents = documents;
        }

        public IList<CrawlDocument> Documents { get; private set; }

        public int HtmlDocs { get { return Documents.Count(x => !x.IsImage); } }

        public int Images { get { return Documents.Count(x => x.IsImage); } }

        /// <summary>
        /// Reference to the actor who should take on the cleared documents
        /// </summary>
        public IActorRef Requestor { get; private set; }

        /// <summary>
        /// The amount of time we think it'll take to crawl this document
        /// based on current workload.
        /// </summary>
        public TimeSpan? EstimatedCrawlTime { get; private set; }
    }
}
