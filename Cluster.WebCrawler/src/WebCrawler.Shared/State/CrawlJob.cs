// -----------------------------------------------------------------------
// <copyright file="CrawlJob.cs" company="Petabridge, LLC">
//      Copyright (C) 2018 - 2018 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace WebCrawler.Shared.State
{
    /// <summary>
    ///     Defines a web crawl operation
    /// </summary>
    public class CrawlJob : IEquatable<CrawlJob>
    {
        public CrawlJob(Uri root, bool fetchImages)
        {
            FetchImages = fetchImages;
            Root = root;
        }

        /// <summary>
        ///     Absolute URL for root document
        /// </summary>
        public Uri Root { get; }

        public string Domain => Root.Host;

        public bool FetchImages { get; }

        public override string ToString()
        {
            return Root.ToString();
        }

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
            if (obj.GetType() != GetType()) return false;
            return Equals((CrawlJob) obj);
        }

        public override int GetHashCode()
        {
            return Root != null ? Root.GetHashCode() : 0;
        }

        #endregion
    }
}