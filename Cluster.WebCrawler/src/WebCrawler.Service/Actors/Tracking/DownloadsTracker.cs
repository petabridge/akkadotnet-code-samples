using System;
using System.Collections.Generic;
using Akka.Actor;
using WebCrawler.Messages.State;
using WebCrawler.Service.Messages;
using WebCrawler.Service.State;

namespace WebCrawler.Service.Actors.Downloads
{
    /// <summary>
    /// Actor responsible for documenting the persistence of documents
    /// </summary>
    public class DownloadsTracker : ReceiveActor
    {
        private readonly Dictionary<CrawlDocument, CrawlStatus> _recordedDocuments;
        private readonly TimeSpan _defaultCrawlTime;

        public DownloadsTracker() : this(new Dictionary<CrawlDocument, CrawlStatus>(), TimeSpan.FromSeconds(30)) { }

        public DownloadsTracker(Dictionary<CrawlDocument, CrawlStatus> recordedDocuments, TimeSpan defaultCrawlTime)
        {
            _recordedDocuments = recordedDocuments;
            _defaultCrawlTime = defaultCrawlTime;
            InitialReceives();
        }

        private void InitialReceives()
        {
            //perform a diff on the documents users want checked versus what's been claimed
            Receive<CheckDocuments>(check =>
            {
                var availableDocs = new List<CrawlDocument>();
                var discoveredDocs = new List<CrawlDocument>();
                foreach (var doc in check.Documents)
                {
                    //first time we've seen this doc
                    if (!_recordedDocuments.ContainsKey(doc))
                    {
                        _recordedDocuments[doc] = CrawlStatus.StartCrawl(check.Requestor, check.EstimatedCrawlTime ?? _defaultCrawlTime);
                        availableDocs.Add(doc);
                        discoveredDocs.Add(doc);
                    }
                    else if(_recordedDocuments[doc].TryClaim(check.Requestor, check.EstimatedCrawlTime ?? _defaultCrawlTime))
                    {
                        //TODO: add status message about new actor taking over processing here
                        availableDocs.Add(doc);
                    }
                }

                Sender.Tell(new ProcessDocuments(availableDocs, check.Requestor));
                Sender.Tell(new DiscoveredDocuments(discoveredDocs, check.Requestor));
            });

            Receive<CompletedDocument>(doc =>
            {
                if (!_recordedDocuments.ContainsKey(doc.Document))
                    _recordedDocuments[doc.Document] = CrawlStatus.StartCrawl(doc.CompletedBy, _defaultCrawlTime);
                _recordedDocuments[doc.Document].MarkAsComplete();
            });
        }
    }
}
