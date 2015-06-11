using System;
using WebCrawler.Messages.State;

namespace WebCrawler.Messages.Commands.V1
{
    public interface IStatusUpdateV1
    {
        CrawlJob Job { get; }
        CrawlJobStats Stats { get; set; }
        DateTime StartTime { get; }
        DateTime? EndTime { get; set; }
        TimeSpan Elapsed { get; }
        JobStatus Status { get; set; }
    }
}