using System.Collections.Generic;
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
        public CheckDocuments(IList<CrawlDocument> documents)
        {
            Documents = documents;
        }

        public IList<CrawlDocument> Documents { get; private set; }
    }
}
