using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Util;

namespace WebCrawler.Service.State
{
    /// <summary>
    /// Represents a single document, regardless of content type, discovered but not downloaded
    /// </summary>
    public class CrawlDocument : IEquatable<CrawlDocument>
    {
        public CrawlDocument(Uri documentUri, bool isImage = false)
        {
            IsImage = isImage;
            Debug.Assert(documentUri.IsAbsoluteUri, "documentUri must be absolute");
            DocumentUri = documentUri;
        }

        /// <summary>
        /// Absolute URI of the document
        /// </summary>
        public Uri DocumentUri { get; private set; }

        public bool IsImage { get; private set; }

        public bool Equals(CrawlDocument other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(DocumentUri, other.DocumentUri);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CrawlDocument) obj);
        }

        public override int GetHashCode()
        {
            return (DocumentUri != null ? DocumentUri.GetHashCode() : 0);
        }

        private sealed class DocumentUriEqualityComparer : IEqualityComparer<CrawlDocument>
        {
            public bool Equals(CrawlDocument x, CrawlDocument y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return Equals(x.DocumentUri, y.DocumentUri);
            }

            public int GetHashCode(CrawlDocument obj)
            {
                return (obj.DocumentUri != null ? obj.DocumentUri.GetHashCode() : 0);
            }
        }

        private static readonly IEqualityComparer<CrawlDocument> DocumentUriComparerInstance = new DocumentUriEqualityComparer();

        public static IEqualityComparer<CrawlDocument> DocumentUriComparer
        {
            get { return DocumentUriComparerInstance; }
        }
    }
}
