using System;
using WebCrawler.Shared.State;

namespace WebCrawler.Shared.Commands.V1
{
    public interface IStatusUpdateV1
    {
        CrawlJob Job { get; }
        CrawlJobStats Stats { get; }
        DateTime StartTime { get; }
        DateTime? EndTime { get; }
        TimeSpan Elapsed { get; }
        JobStatus Status { get; }
    }
}