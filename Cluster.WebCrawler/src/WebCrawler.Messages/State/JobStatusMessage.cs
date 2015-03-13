namespace WebCrawler.Messages.State
{
    /// <summary>
    /// Used to report on the status of a <see cref="CrawlJob"/>
    /// </summary>
    public class JobStatusMessage
    {
        public JobStatusMessage(CrawlJob job, string documentTitle, string message)
        {
            Message = message;
            DocumentTitle = documentTitle;
            Job = job;
        }

        public CrawlJob Job { get; private set; }

        public string DocumentTitle { get; private set; }

        public string Message { get; private set; }

        public override string ToString()
        {
            return string.Format("[{0}][{1}][{2}]", Job, DocumentTitle, Message);
        }
    }
}
