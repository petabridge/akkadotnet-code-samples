// -----------------------------------------------------------------------
// <copyright file="JobStatusMessage.cs" company="Petabridge, LLC">
//      Copyright (C) 2018 - 2018 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace WebCrawler.Shared.State
{
    /// <summary>
    ///     Used to report on the status of a <see cref="CrawlJob" />
    /// </summary>
    public class JobStatusMessage
    {
        public JobStatusMessage(CrawlJob job, string documentTitle, string message)
        {
            Message = message;
            DocumentTitle = documentTitle;
            Job = job;
        }

        public CrawlJob Job { get; }

        public string DocumentTitle { get; }

        public string Message { get; }

        public override string ToString()
        {
            return string.Format("[{0}][{1}][{2}]", Job, DocumentTitle, Message);
        }
    }
}