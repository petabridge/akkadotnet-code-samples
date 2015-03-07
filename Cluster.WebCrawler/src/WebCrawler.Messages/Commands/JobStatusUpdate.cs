using System;
using WebCrawler.Messages.State;

namespace WebCrawler.Messages.Commands
{
    public enum JobStatus
    {
        Running = 0,
        Starting = 1,
        Failed = 2,
        Finished = 3,
        Stopped = 4
    }

    public class JobStatusUpdate
    {
        public JobStatusUpdate(CrawlJob job)
        {
            Job = job;
            StartTime = DateTime.UtcNow;
            Status = JobStatus.Starting;
        }

        public CrawlJob Job { get; private set; }

        public CrawlJobStats Stats { get; set; }

        public DateTime StartTime { get; private set; }

        public DateTime? EndTime { get; set; }

        public TimeSpan Elapsed
        {
            get
            {
                return ((EndTime.HasValue ? EndTime.Value : DateTime.UtcNow) - StartTime);
            }
        }

        public JobStatus Status { get; set; }
    }
}
