// -----------------------------------------------------------------------
// <copyright file="CompletedDocument.cs" company="Petabridge, LLC">
//      Copyright (C) 2018 - 2018 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace WebCrawler.Shared.State
{
    /// <summary>
    ///     Represents a downloaded <see cref="CrawlDocument" />
    /// </summary>
    public class CompletedDocument
    {
        public CompletedDocument(CrawlDocument document, int numBytes, IActorRef completedBy)
        {
            CompletedBy = completedBy;
            NumBytes = numBytes;
            Document = document;
        }

        public CrawlDocument Document { get; }

        public int NumBytes { get; }

        public IActorRef CompletedBy { get; }
    }

    /// <summary>
    ///     Represents new <see cref="CrawlDocument" />s discovered by a parsing operation
    /// </summary>
    public class DiscoveredDocuments
    {
        public DiscoveredDocuments(IList<CrawlDocument> documents, IActorRef discoveredBy)
        {
            DiscoveredBy = discoveredBy;
            Documents = documents;
        }

        public IList<CrawlDocument> Documents { get; }

        public int HtmlDocs
        {
            get { return Documents.Count(x => !x.IsImage); }
        }

        public int Images
        {
            get { return Documents.Count(x => x.IsImage); }
        }

        public IActorRef DiscoveredBy { get; }
    }
}