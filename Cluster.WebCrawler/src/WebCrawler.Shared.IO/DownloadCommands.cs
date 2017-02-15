using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using WebCrawler.TrackingService.State;

namespace WebCrawler.Shared.IO
{
    public interface IDownloadResult
    {
        IDownloadDocument Command { get; }
        HttpStatusCode Status { get; }
    }

    /// <summary>
    /// Results form a <see cref="DownloadHtmlDocument"/> operation
    /// </summary>
    public class DownloadHtmlResult : IDownloadResult
    {
        public DownloadHtmlResult(DownloadHtmlDocument command, string content, HttpStatusCode status)
        {
            Status = status;
            Content = content;
            Command = command;
        }

        public string Content { get; }

        public IDownloadDocument Command { get; }
        public HttpStatusCode Status { get; }
    }

    public class DownloadImage : IDownloadDocument, IEquatable<DownloadImage>
    {
        public DownloadImage(CrawlDocument document)
        {
            Document = document;
        }

        public CrawlDocument Document { get; }

        public bool Equals(DownloadImage other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Document, other.Document);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DownloadImage)obj);
        }

        public override int GetHashCode()
        {
            return Document?.GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// Results from a <see cref="DownloadImage"/> operation
    /// </summary>
    public class DownloadImageResult : IDownloadResult
    {
        public DownloadImageResult(DownloadImage command, byte[] bytes, HttpStatusCode status)
        {
            Status = status;
            Bytes = bytes;
            Command = command;
        }

        public IDownloadDocument Command { get; private set; }

        public byte[] Bytes { get; private set; }

        public HttpStatusCode Status { get; private set; }
    }

    public class DownloadHtmlDocument : IDownloadDocument, IEquatable<DownloadHtmlDocument>
    {
        public DownloadHtmlDocument(CrawlDocument document)
        {
            Document = document;
        }

        public CrawlDocument Document { get; }

        public bool Equals(DownloadHtmlDocument other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Document, other.Document);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DownloadHtmlDocument)obj);
        }

        public override int GetHashCode()
        {
            return Document?.GetHashCode() ?? 0;
        }
    }

    public interface IDownloadDocument
    {
        CrawlDocument Document { get; }
    }
}
