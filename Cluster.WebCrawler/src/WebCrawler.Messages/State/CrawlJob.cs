using System;

namespace WebCrawler.Messages.State
{
    /// <summary>
    /// Defines a web crawl operation
    /// </summary>
    public class CrawlJob : IEquatable<CrawlJob>
    {
        public CrawlJob(Uri root, bool fetchImages)
        {
            FetchImages = fetchImages;
            Root = root;
        }

        /// <summary>
        /// Absolute URL for root document
        /// </summary>
        public Uri Root { get; private set; }

        public string Domain { get { return Root.Host; } }

        public bool FetchImages { get; private set; }

        #region Equality

        public bool Equals(CrawlJob other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Root, other.Root);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CrawlJob) obj);
        }

        public override int GetHashCode()
        {
            return (Root != null ? Root.GetHashCode() : 0);
        }

        #endregion

        public override string ToString()
        {
            return Root.ToString();
        }
    }
}
